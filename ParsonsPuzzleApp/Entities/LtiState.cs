namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Temporary storage for OIDC state and nonce during LTI 1.3 launch flow.
    /// These records are created during login initiation and consumed during launch.
    /// </summary>
    public class LtiState
    {
        public int Id { get; set; }

        /// <summary>
        /// The state parameter used in OIDC authorization request.
        /// Used to correlate login initiation with launch callback.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The nonce included in the authentication request.
        /// Must match the nonce in the ID token to prevent replay attacks.
        /// </summary>
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// The platform's client ID this state is associated with
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The target resource link URI from the login initiation
        /// </summary>
        public string? TargetLinkUri { get; set; }

        /// <summary>
        /// When this state record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this state expires (typically 10 minutes after creation)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether this state has been consumed (used in a successful launch)
        /// </summary>
        public bool IsUsed { get; set; } = false;
    }
}
