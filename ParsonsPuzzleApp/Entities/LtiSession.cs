namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Tracks each LTI launch session including grade passback status and return URL.
    /// </summary>
    public class LtiSession
    {
        public int Id { get; set; }

        /// <summary>
        /// LTI subject identifier (sub claim) for the student
        /// </summary>
        public string? UserId { get; set; }

        public int LtiPlatformId { get; set; }
        public LtiPlatform Platform { get; set; } = null!;

        public string DeploymentId { get; set; } = string.Empty;

        public string? ResourceLinkId { get; set; }

        /// <summary>
        /// The bundle attempt ID generated for this LTI session
        /// </summary>
        public Guid BundleAttemptId { get; set; }

        public int BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;

        /// <summary>
        /// Return URL from the LTI launch_presentation claim (e.g., Moodle course page)
        /// </summary>
        public string? ReturnUrl { get; set; }

        public string? ContextTitle { get; set; }

        public DateTime LaunchedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public bool GradeSent { get; set; } = false;

        public DateTime? GradeSentAt { get; set; }
    }
}
