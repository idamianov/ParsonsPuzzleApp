using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Constants;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class EditLtiPlatformModel : PageModel
    {
        private readonly ILtiService _ltiService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditLtiPlatformModel(
            ILtiService ltiService,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _ltiService = ltiService;
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public LtiPlatformInputModel Input { get; set; } = new();

        public LtiDeploymentInputModel DeploymentInput { get; set; } = new();

        public LtiPlatform Platform { get; set; } = null!;
        public List<SelectListItem> AvailableBundles { get; set; } = new();
        public string? SetupDeploymentId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = await _ltiService.GetPlatformByIdAsync(id, cancellationToken);

            if (platform == null || platform.InstructorId != userId)
            {
                return NotFound();
            }

            Platform = platform;

            Input = new LtiPlatformInputModel
            {
                Id = platform.Id,
                Name = platform.Name,
                Issuer = platform.Issuer,
                ClientId = platform.ClientId,
                AuthorizationEndpoint = platform.AuthorizationEndpoint,
                TokenEndpoint = platform.TokenEndpoint,
                JwksUrl = platform.JwksUrl,
                IsActive = platform.IsActive
            };

            await LoadAvailableBundlesAsync(userId, cancellationToken);

            // Check for setup deployment from LTI launch
            SetupDeploymentId = HttpContext.Session.GetString(LtiSessionKeys.SetupDeploymentId);
            var setupPlatformId = HttpContext.Session.GetString(LtiSessionKeys.SetupPlatformId);
            if (setupPlatformId == id.ToString())
            {
                HttpContext.Session.Remove(LtiSessionKeys.SetupDeploymentId);
                HttpContext.Session.Remove(LtiSessionKeys.SetupPlatformId);
                DeploymentInput.DeploymentId = SetupDeploymentId ?? string.Empty;
            }
            else
            {
                SetupDeploymentId = null;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = await _ltiService.GetPlatformByIdAsync(Input.Id, cancellationToken);

            if (platform == null || platform.InstructorId != userId)
            {
                return NotFound();
            }

            Platform = platform;
            await LoadAvailableBundlesAsync(userId, cancellationToken);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            platform.Name = Input.Name;
            platform.Issuer = Input.Issuer.TrimEnd('/');
            platform.ClientId = Input.ClientId;
            platform.AuthorizationEndpoint = Input.AuthorizationEndpoint;
            platform.TokenEndpoint = Input.TokenEndpoint;
            platform.JwksUrl = Input.JwksUrl;
            platform.IsActive = Input.IsActive;

            await _ltiService.UpdatePlatformAsync(platform, cancellationToken);

            TempData["SuccessMessage"] = "Platform updated successfully.";
            return RedirectToPage(new { id = Input.Id });
        }

        public async Task<IActionResult> OnPostAddDeploymentAsync(int id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = await _ltiService.GetPlatformByIdAsync(id, cancellationToken);

            if (platform == null || platform.InstructorId != userId)
            {
                return NotFound();
            }

            Platform = platform;
            await LoadAvailableBundlesAsync(userId, cancellationToken);

            // Manually bind the deployment input for this handler only
            await TryUpdateModelAsync(DeploymentInput, nameof(DeploymentInput));

            // Validate deployment input
            if (string.IsNullOrWhiteSpace(DeploymentInput.DeploymentId))
            {
                ModelState.AddModelError("DeploymentInput.DeploymentId", "Deployment ID is required");
                return Page();
            }

            // Check if deployment already exists
            var existingDeployment = platform.Deployments?
                .FirstOrDefault(d => d.DeploymentId == DeploymentInput.DeploymentId);

            if (existingDeployment != null)
            {
                ModelState.AddModelError("DeploymentInput.DeploymentId", "A deployment with this ID already exists");
                return Page();
            }

            var deployment = new LtiDeployment
            {
                DeploymentId = DeploymentInput.DeploymentId,
                Name = DeploymentInput.Name,
                BundleId = DeploymentInput.BundleId,
                LtiPlatformId = platform.Id,
                IsActive = true
            };

            await _ltiService.CreateDeploymentAsync(deployment, cancellationToken);

            TempData["SuccessMessage"] = "Deployment added successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdateDeploymentAsync(int id, int deploymentId, int? bundleId, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = await _ltiService.GetPlatformByIdAsync(id, cancellationToken);
            if (platform == null || platform.InstructorId != userId)
            {
                return NotFound();
            }

            var deployment = platform.Deployments?.FirstOrDefault(d => d.Id == deploymentId);
            if (deployment == null)
            {
                return NotFound();
            }

            deployment.BundleId = bundleId;
            await _ltiService.UpdateDeploymentAsync(deployment, cancellationToken);

            TempData["SuccessMessage"] = "Deployment updated successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteDeploymentAsync(int id, int deploymentId, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            await _ltiService.DeleteDeploymentAsync(deploymentId, userId, cancellationToken);

            TempData["SuccessMessage"] = "Deployment deleted successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostToggleDeploymentAsync(int id, int deploymentId, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = await _ltiService.GetPlatformByIdAsync(id, cancellationToken);
            if (platform == null || platform.InstructorId != userId)
            {
                return NotFound();
            }

            var deployment = platform.Deployments?.FirstOrDefault(d => d.Id == deploymentId);
            if (deployment == null)
            {
                return NotFound();
            }

            deployment.IsActive = !deployment.IsActive;
            await _ltiService.UpdateDeploymentAsync(deployment, cancellationToken);

            return RedirectToPage(new { id });
        }

        private async Task LoadAvailableBundlesAsync(string userId, CancellationToken cancellationToken)
        {
            var bundles = await _context.Bundles
                .Where(b => b.InstructorId == userId && b.IsPublished)
                .OrderBy(b => b.Identifier)
                .ToListAsync(cancellationToken);

            AvailableBundles = bundles.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Identifier} - {b.Description}"
            }).ToList();

            AvailableBundles.Insert(0, new SelectListItem { Value = "", Text = "(Not linked)" });
        }
    }
}
