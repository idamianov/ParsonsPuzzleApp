namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Represents an LTI 1.3 Platform (LMS) registration.
    /// Each platform has a unique issuer and can have multiple deployments.
    /// </summary>
    public class LtiPlatform
    {
        public int Id { get; set; }

        /// <summary>
        /// Display name for the platform (e.g., "University Moodle", "Blackboard Learn")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The platform's issuer identifier (iss claim in JWT).
        /// Usually the platform's base URL (e.g., "https://moodle.university.edu")
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 Client ID assigned by the platform during registration
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Platform's OIDC authorization endpoint
        /// </summary>
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Platform's OAuth 2.0 token endpoint (for service calls like AGS)
        /// </summary>
        public string TokenEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Platform's JSON Web Key Set URL for verifying platform signatures
        /// </summary>
        public string JwksUrl { get; set; } = string.Empty;

        /// <summary>
        /// Whether this platform registration is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Instructor who registered this platform
        /// </summary>
        public string InstructorId { get; set; } = string.Empty;

        // Navigation properties
        public List<LtiDeployment> Deployments { get; set; } = new List<LtiDeployment>();
    }
}
