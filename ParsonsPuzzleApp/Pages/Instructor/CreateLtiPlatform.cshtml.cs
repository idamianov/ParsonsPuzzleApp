using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class CreateLtiPlatformModel : PageModel
    {
        private readonly ILtiService _ltiService;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateLtiPlatformModel(ILtiService ltiService, UserManager<IdentityUser> userManager)
        {
            _ltiService = ltiService;
            _userManager = userManager;
        }

        [BindProperty]
        public LtiPlatformInputModel Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            var platform = new LtiPlatform
            {
                Name = Input.Name,
                Issuer = Input.Issuer.TrimEnd('/'),
                ClientId = Input.ClientId,
                AuthorizationEndpoint = Input.AuthorizationEndpoint,
                TokenEndpoint = Input.TokenEndpoint,
                JwksUrl = Input.JwksUrl,
                IsActive = Input.IsActive,
                InstructorId = userId
            };

            await _ltiService.CreatePlatformAsync(platform, cancellationToken);

            return RedirectToPage("EditLtiPlatform", new { id = platform.Id });
        }
    }
}
