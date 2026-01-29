using Microsoft.IdentityModel.Tokens;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    /// <summary>
    /// Service for handling LTI 1.3 operations including OIDC login, JWT validation, and launch processing.
    /// </summary>
    public interface ILtiService
    {
        /// <summary>
        /// Gets the JSON Web Key Set for this tool (public keys for platform verification)
        /// </summary>
        JsonWebKeySet GetJwks();

        /// <summary>
        /// Creates a new OIDC state record for the login initiation flow
        /// </summary>
        /// <param name="clientId">The platform's client ID</param>
        /// <param name="targetLinkUri">The target resource link URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created state record with state and nonce values</returns>
        Task<LtiState> CreateStateAsync(string clientId, string? targetLinkUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and consumes a state record during launch
        /// </summary>
        /// <param name="state">The state value from the launch request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The state record if valid, null if not found or expired</returns>
        Task<LtiState?> ValidateAndConsumeStateAsync(string state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an LTI 1.3 ID token from a platform
        /// </summary>
        /// <param name="idToken">The JWT ID token</param>
        /// <param name="expectedNonce">The expected nonce value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validated launch result, or null if validation fails</returns>
        Task<LtiLaunchResult?> ValidateIdTokenAsync(string idToken, string expectedNonce, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a platform registration by issuer and client ID
        /// </summary>
        Task<LtiPlatform?> GetPlatformAsync(string issuer, string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a platform by ID
        /// </summary>
        Task<LtiPlatform?> GetPlatformByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a deployment by platform ID and deployment ID
        /// </summary>
        Task<LtiDeployment?> GetDeploymentAsync(int platformId, string deploymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new platform registration
        /// </summary>
        Task<LtiPlatform> CreatePlatformAsync(LtiPlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing platform registration
        /// </summary>
        Task<LtiPlatform> UpdatePlatformAsync(LtiPlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a platform registration
        /// </summary>
        Task DeletePlatformAsync(int platformId, string instructorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new deployment for a platform
        /// </summary>
        Task<LtiDeployment> CreateDeploymentAsync(LtiDeployment deployment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing deployment
        /// </summary>
        Task<LtiDeployment> UpdateDeploymentAsync(LtiDeployment deployment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a deployment
        /// </summary>
        Task DeleteDeploymentAsync(int deploymentId, string instructorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all platforms for an instructor
        /// </summary>
        Task<List<LtiPlatform>> GetPlatformsForInstructorAsync(string instructorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up expired state records
        /// </summary>
        Task CleanupExpiredStatesAsync(CancellationToken cancellationToken = default);
    }
}
