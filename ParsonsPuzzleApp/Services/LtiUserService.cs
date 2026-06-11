using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public class LtiUserService : ILtiUserService
    {
        private const string LtiSubjectClaimType = "lti_subject";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LtiUserService> _logger;

        public LtiUserService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<LtiUserService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IdentityUser> GetOrCreateLtiUserAsync(LtiLaunchResult launchResult, CancellationToken ct = default)
        {
            // Stable identity key: issuer|subject
            var ltiSubjectValue = $"{launchResult.Issuer}|{launchResult.Subject}";

            // 1. Try find by lti_subject claim (most reliable — works across email changes)
            var usersWithClaim = await _userManager.GetUsersForClaimAsync(
                new Claim(LtiSubjectClaimType, ltiSubjectValue));

            if (usersWithClaim.Count > 0)
            {
                _logger.LogDebug("Found existing LTI user by subject claim: {Subject}", ltiSubjectValue);
                return usersWithClaim[0];
            }

            // 2. Try find by email (first launch, claim not yet stored)
            if (!string.IsNullOrEmpty(launchResult.Email))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(launchResult.Email);
                if (existingByEmail != null)
                {
                    await _userManager.AddClaimAsync(existingByEmail, new Claim(LtiSubjectClaimType, ltiSubjectValue));
                    _logger.LogInformation("Linked existing account {Email} to LTI subject {Subject}",
                        launchResult.Email, ltiSubjectValue);
                    return existingByEmail;
                }
            }

            // 3. Create new user
            var userName = $"lti_{Guid.NewGuid():N}";
            var email = launchResult.Email ?? $"{userName}@lti.local";

            var newUser = new IdentityUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create LTI user: {errors}");
            }

            await _userManager.AddClaimAsync(newUser, new Claim(LtiSubjectClaimType, ltiSubjectValue));

            _logger.LogInformation("Created new LTI user {UserName} for subject {Subject}",
                userName, ltiSubjectValue);

            return newUser;
        }

        public async Task SignInLtiUserAsync(IdentityUser user)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogDebug("Signed in LTI user {UserId}", user.Id);
        }
    }
}
