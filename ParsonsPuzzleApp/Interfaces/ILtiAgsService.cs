using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    /// <summary>
    /// Service for sending grades back to the LMS via LTI Advantage Grade Services (AGS).
    /// </summary>
    public interface ILtiAgsService
    {
        /// <summary>
        /// Sends the student's grade for the given LTI session to the LMS.
        /// Returns true if grade was sent successfully.
        /// </summary>
        Task<bool> SendGradeAsync(int ltiSessionId, CancellationToken ct = default);

        /// <summary>
        /// Gets a cached or fresh OAuth 2.0 access token for the given platform and scope.
        /// </summary>
        Task<string?> GetAccessTokenAsync(LtiPlatform platform, string scope, CancellationToken ct = default);
    }
}
