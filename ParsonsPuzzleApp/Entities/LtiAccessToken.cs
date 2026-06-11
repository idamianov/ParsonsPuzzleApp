namespace ParsonsPuzzleApp.Entities
{
    /// <summary>
    /// Caches OAuth 2.0 access tokens for AGS calls to avoid repeated token requests.
    /// </summary>
    public class LtiAccessToken
    {
        public int Id { get; set; }

        public int LtiPlatformId { get; set; }
        public LtiPlatform Platform { get; set; } = null!;

        /// <summary>
        /// The OAuth scope this token was issued for
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
