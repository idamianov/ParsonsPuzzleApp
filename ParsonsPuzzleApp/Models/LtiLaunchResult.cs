namespace ParsonsPuzzleApp.Models
{
    /// <summary>
    /// Contains the validated result of an LTI 1.3 launch request.
    /// </summary>
    public class LtiLaunchResult
    {
        /// <summary>
        /// The platform's issuer identifier (iss claim)
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The client ID (audience/aud claim)
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The user's subject identifier (sub claim)
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// The deployment ID for this launch
        /// </summary>
        public string DeploymentId { get; set; } = string.Empty;

        /// <summary>
        /// The resource link ID (identifies the specific link in the platform)
        /// </summary>
        public string? ResourceLinkId { get; set; }

        /// <summary>
        /// User's full name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// User's given (first) name
        /// </summary>
        public string? GivenName { get; set; }

        /// <summary>
        /// User's family (last) name
        /// </summary>
        public string? FamilyName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User's roles in the context (e.g., Instructor, Learner)
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Context ID (typically course ID)
        /// </summary>
        public string? ContextId { get; set; }

        /// <summary>
        /// Context title (typically course name)
        /// </summary>
        public string? ContextTitle { get; set; }

        /// <summary>
        /// Gets the user's display name, falling back to email or subject if name is not available
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(GivenName))
                    parts.Add(GivenName);
                if (!string.IsNullOrEmpty(FamilyName))
                    parts.Add(FamilyName);

                if (parts.Count > 0)
                    return string.Join(" ", parts);

                return Email ?? Subject ?? "Unknown User";
            }
        }

        /// <summary>
        /// Checks if the user has an instructor role
        /// </summary>
        public bool IsInstructor => Roles.Any(r =>
            r.Contains("Instructor", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Administrator", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("ContentDeveloper", StringComparison.OrdinalIgnoreCase));
    }
}
