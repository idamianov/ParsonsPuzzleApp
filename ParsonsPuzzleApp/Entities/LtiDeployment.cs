namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Represents a specific deployment of the tool within an LTI Platform.
    /// A platform can have multiple deployments (e.g., different courses or institutions).
    /// </summary>
    public class LtiDeployment
    {
        public int Id { get; set; }

        /// <summary>
        /// The deployment ID assigned by the platform.
        /// This is unique within the platform and identifies where the tool is deployed.
        /// </summary>
        public string DeploymentId { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name for this deployment
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The bundle that this deployment links to.
        /// When users launch from this deployment, they access this bundle.
        /// </summary>
        public int? BundleId { get; set; }

        /// <summary>
        /// Whether this deployment is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        // Foreign key to platform
        public int LtiPlatformId { get; set; }

        // Navigation properties
        public LtiPlatform Platform { get; set; } = null!;
        public Bundle? Bundle { get; set; }
    }
}
