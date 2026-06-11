using System.ComponentModel.DataAnnotations.Schema;

namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Stores AGS endpoint URLs per resource link for grade passback.
    /// </summary>
    public class LtiResourceLink
    {
        public int Id { get; set; }

        public int LtiPlatformId { get; set; }
        public LtiPlatform Platform { get; set; } = null!;

        public string DeploymentId { get; set; } = string.Empty;

        public string ResourceLinkId { get; set; } = string.Empty;

        /// <summary>
        /// The specific line item URL for this resource (used for direct score posting)
        /// </summary>
        public string? LineItemUrl { get; set; }

        /// <summary>
        /// The line items container URL (used to discover or create line items)
        /// </summary>
        public string? LineItemsUrl { get; set; }

        /// <summary>
        /// AGS scopes granted for this resource link, stored as semicolon-separated string
        /// </summary>
        public string? ScopesRaw { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string[]? Scopes
        {
            get => string.IsNullOrEmpty(ScopesRaw) ? null : ScopesRaw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            set => ScopesRaw = value == null ? null : string.Join(';', value);
        }
    }
}
