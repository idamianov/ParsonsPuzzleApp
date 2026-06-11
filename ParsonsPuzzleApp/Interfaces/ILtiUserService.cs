using Microsoft.AspNetCore.Identity;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface ILtiUserService
    {
        Task<IdentityUser> GetOrCreateLtiUserAsync(LtiLaunchResult launchResult, CancellationToken ct = default);
        Task SignInLtiUserAsync(IdentityUser user);
    }
}
