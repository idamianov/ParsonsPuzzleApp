namespace ParsonsPuzzleApp.Models
{
    /// <summary>
    /// Configuration options for LTI 1.3 tool settings.
    /// Bound to the "Lti" section in appsettings.json.
    /// </summary>
    public class LtiOptions
    {
        /// <summary>
        /// The tool's unique identifier (used as client_id in some configurations)
        /// </summary>
        public string ToolId { get; set; } = "parsons-puzzle-app";

        /// <summary>
        /// Base URL of this tool (used for generating callback URLs)
        /// </summary>
        public string ToolBaseUrl { get; set; } = "https://localhost:5001";

        /// <summary>
        /// Path to the RSA private key file (PEM format) for signing JWTs.
        /// If not specified, a key will be generated and stored.
        /// </summary>
        public string? PrivateKeyPath { get; set; }

        /// <summary>
        /// The RSA private key in PEM format (alternative to PrivateKeyPath).
        /// Can be set directly or via environment variable.
        /// </summary>
        public string? PrivateKeyPem { get; set; }

        /// <summary>
        /// Key ID (kid) for the current signing key
        /// </summary>
        public string KeyId { get; set; } = "parsons-puzzle-key-1";

        /// <summary>
        /// How long state records should be valid (in minutes)
        /// </summary>
        public int StateExpirationMinutes { get; set; } = 10;

        /// <summary>
        /// Whether to allow launches from unregistered platforms (for testing)
        /// </summary>
        public bool AllowUnregisteredPlatforms { get; set; } = false;

        // Computed URLs based on ToolBaseUrl
        public string LoginUrl => $"{ToolBaseUrl}/lti/login";
        public string LaunchUrl => $"{ToolBaseUrl}/lti/launch";
        public string JwksUrl => $"{ToolBaseUrl}/.well-known/jwks.json";
        public string DeepLinkingResponseUrl => $"{ToolBaseUrl}/lti/deeplink";
    }
}
