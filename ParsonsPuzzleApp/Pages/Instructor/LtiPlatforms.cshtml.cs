using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParsonsPuzzleApp.Constants;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class LtiPlatformsModel : PageModel
    {
        private readonly ILtiService _ltiService;
        private readonly UserManager<IdentityUser> _userManager;

        public LtiPlatformsModel(ILtiService ltiService, UserManager<IdentityUser> userManager)
        {
            _ltiService = ltiService;
            _userManager = userManager;
        }

        public List<LtiPlatform> Platforms { get; set; } = new();
        public string? SetupMessage { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return;
            }

            Platforms = await _ltiService.GetPlatformsForInstructorAsync(userId, cancellationToken);

            // Check for LTI setup message from session
            SetupMessage = HttpContext.Session.GetString(LtiSessionKeys.SetupMessage);
            if (!string.IsNullOrEmpty(SetupMessage))
            {
                HttpContext.Session.Remove(LtiSessionKeys.SetupMessage);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }

            await _ltiService.DeletePlatformAsync(id, userId, cancellationToken);

            return RedirectToPage();
        }
    }
}
