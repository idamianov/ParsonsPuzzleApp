using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using LtiAdvantage.Lti;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    /// <summary>
    /// Service for handling LTI 1.3 operations including OIDC login, JWT validation, and launch processing.
    /// </summary>
    public class LtiService : ILtiService
    {
        private readonly ApplicationDbContext _context;
        private readonly LtiOptions _options;
        private readonly ILogger<LtiService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILtiKeyProvider _keyProvider;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan JwksCacheDuration = TimeSpan.FromHours(1);

        public LtiService(
            ApplicationDbContext context,
            IOptions<LtiOptions> options,
            ILogger<LtiService> logger,
            IHttpClientFactory httpClientFactory,
            ILtiKeyProvider keyProvider,
            IMemoryCache cache)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keyProvider = keyProvider;
            _cache = cache;
        }

        public JsonWebKeySet GetJwks()
        {
            return _keyProvider.GetJwks();
        }

        public async Task<LtiState> CreateStateAsync(string clientId, string? targetLinkUri, CancellationToken cancellationToken = default)
        {
            var state = new LtiState
            {
                State = GenerateSecureToken(),
                Nonce = GenerateSecureToken(),
                ClientId = clientId,
                TargetLinkUri = targetLinkUri,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.StateExpirationMinutes),
                IsUsed = false
            };

            _context.LtiStates.Add(state);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Created LTI state: {State} for client {ClientId}", state.State, clientId);
            return state;
        }

        public async Task<LtiState?> ValidateAndConsumeStateAsync(string state, CancellationToken cancellationToken = default)
        {
            var ltiState = await _context.LtiStates
                .FirstOrDefaultAsync(s => s.State == state && !s.IsUsed && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (ltiState == null)
            {
                _logger.LogWarning("Invalid or expired state: {State}", state);
                return null;
            }

            ltiState.IsUsed = true;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Consumed LTI state: {State}", state);
            return ltiState;
        }

        public async Task<LtiLaunchResult?> ValidateIdTokenAsync(string idToken, string expectedNonce, CancellationToken cancellationToken = default)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(idToken);

                // Extract issuer and client_id (audience) to find the platform
                var issuer = jwt.Issuer;
                var clientId = jwt.Audiences.FirstOrDefault();

                if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(clientId))
                {
                    _logger.LogWarning("ID token missing issuer or audience");
                    return null;
                }

                // Find the platform registration
                var platform = await GetPlatformAsync(issuer, clientId, cancellationToken);
                if (platform == null)
                {
                    _logger.LogWarning("Unknown platform: issuer={Issuer}, clientId={ClientId}", issuer, clientId);
                    return null;
                }

                // Fetch platform's JWKS for signature verification (with caching)
                var platformKeys = await FetchPlatformJwksAsync(platform.JwksUrl, cancellationToken);
                if (platformKeys == null)
                {
                    _logger.LogError("Failed to fetch platform JWKS from {Url}", platform.JwksUrl);
                    return null;
                }

                // Validate the token
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = clientId,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = platformKeys.Keys,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                handler.ValidateToken(idToken, validationParameters, out _);

                // Verify nonce
                var tokenNonce = jwt.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
                if (tokenNonce != expectedNonce)
                {
                    _logger.LogWarning("Nonce mismatch: expected={Expected}, actual={Actual}", expectedNonce, tokenNonce);
                    return null;
                }

                // Parse into LTI resource link request using LtiAdvantage
                var ltiRequest = new LtiResourceLinkRequest(jwt.Payload);

                // Build our result DTO
                var result = new LtiLaunchResult
                {
                    Issuer = issuer,
                    ClientId = clientId,
                    Subject = jwt.Subject ?? string.Empty,
                    DeploymentId = ltiRequest.DeploymentId ?? string.Empty,
                    ResourceLinkId = ltiRequest.ResourceLink?.Id,
                    Name = ltiRequest.Name,
                    GivenName = ltiRequest.GivenName,
                    FamilyName = ltiRequest.FamilyName,
                    Email = ltiRequest.Email,
                    Roles = ltiRequest.Roles?.Select(r => r.ToString()).ToList() ?? new List<string>(),
                    ContextId = ltiRequest.Context?.Id,
                    ContextTitle = ltiRequest.Context?.Title
                };

                result.ReturnUrl = ExtractReturnUrl(jwt.Claims);
                result.AgsEndpoint = ParseAgsEndpoint(jwt.Claims);

                _logger.LogInformation("Successfully validated LTI launch for user {UserId} from {Platform}",
                    result.Subject, platform.Name);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate ID token");
                return null;
            }
        }

        private async Task<JsonWebKeySet?> FetchPlatformJwksAsync(string jwksUrl, CancellationToken cancellationToken)
        {
            var cacheKey = $"lti_jwks:{jwksUrl}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = JwksCacheDuration;

                try
                {
                    var httpClient = _httpClientFactory.CreateClient("LtiPlatform");
                    var response = await httpClient.GetStringAsync(jwksUrl, cancellationToken);
                    _logger.LogDebug("Fetched and cached JWKS from {Url}", jwksUrl);
                    return new JsonWebKeySet(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch JWKS from {Url}", jwksUrl);
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30); // Short cache for failures
                    return null;
                }
            });
        }

        public async Task<LtiPlatform?> GetPlatformAsync(string issuer, string clientId, CancellationToken cancellationToken = default)
        {
            return await _context.LtiPlatforms
                .Include(p => p.Deployments)
                .FirstOrDefaultAsync(p => p.Issuer == issuer && p.ClientId == clientId && p.IsActive, cancellationToken);
        }

        public async Task<LtiPlatform?> GetPlatformByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.LtiPlatforms
                .Include(p => p.Deployments)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<LtiDeployment?> GetDeploymentAsync(int platformId, string deploymentId, CancellationToken cancellationToken = default)
        {
            return await _context.LtiDeployments
                .Include(d => d.Platform)
                .Include(d => d.Bundle)
                .FirstOrDefaultAsync(d => d.LtiPlatformId == platformId && d.DeploymentId == deploymentId && d.IsActive, cancellationToken);
        }

        public async Task<LtiPlatform> CreatePlatformAsync(LtiPlatform platform, CancellationToken cancellationToken = default)
        {
            platform.CreatedAt = DateTime.UtcNow;
            _context.LtiPlatforms.Add(platform);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created LTI platform: {Name} ({Issuer})", platform.Name, platform.Issuer);
            return platform;
        }

        public async Task<LtiPlatform> UpdatePlatformAsync(LtiPlatform platform, CancellationToken cancellationToken = default)
        {
            platform.LastModifiedAt = DateTime.UtcNow;
            _context.LtiPlatforms.Update(platform);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated LTI platform: {Name} ({Issuer})", platform.Name, platform.Issuer);
            return platform;
        }

        public async Task DeletePlatformAsync(int platformId, string instructorId, CancellationToken cancellationToken = default)
        {
            var platform = await _context.LtiPlatforms
                .FirstOrDefaultAsync(p => p.Id == platformId && p.InstructorId == instructorId, cancellationToken);

            if (platform != null)
            {
                _context.LtiPlatforms.Remove(platform);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted LTI platform: {Name} ({Id})", platform.Name, platform.Id);
            }
        }

        public async Task<LtiDeployment> CreateDeploymentAsync(LtiDeployment deployment, CancellationToken cancellationToken = default)
        {
            deployment.CreatedAt = DateTime.UtcNow;
            _context.LtiDeployments.Add(deployment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created LTI deployment: {DeploymentId} for platform {PlatformId}",
                deployment.DeploymentId, deployment.LtiPlatformId);
            return deployment;
        }

        public async Task<LtiDeployment> UpdateDeploymentAsync(LtiDeployment deployment, CancellationToken cancellationToken = default)
        {
            deployment.LastModifiedAt = DateTime.UtcNow;
            _context.LtiDeployments.Update(deployment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated LTI deployment: {DeploymentId}", deployment.DeploymentId);
            return deployment;
        }

        public async Task DeleteDeploymentAsync(int deploymentId, string instructorId, CancellationToken cancellationToken = default)
        {
            var deployment = await _context.LtiDeployments
                .Include(d => d.Platform)
                .FirstOrDefaultAsync(d => d.Id == deploymentId && d.Platform.InstructorId == instructorId, cancellationToken);

            if (deployment != null)
            {
                _context.LtiDeployments.Remove(deployment);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted LTI deployment: {DeploymentId}", deployment.DeploymentId);
            }
        }

        public async Task<List<LtiPlatform>> GetPlatformsForInstructorAsync(string instructorId, CancellationToken cancellationToken = default)
        {
            return await _context.LtiPlatforms
                .Include(p => p.Deployments)
                    .ThenInclude(d => d.Bundle)
                .Where(p => p.InstructorId == instructorId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task CleanupExpiredStatesAsync(CancellationToken cancellationToken = default)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            var expiredStates = await _context.LtiStates
                .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsUsed)
                .Where(s => s.CreatedAt < cutoffTime) // Keep recent used states for debugging
                .ToListAsync(cancellationToken);

            if (expiredStates.Count > 0)
            {
                _context.LtiStates.RemoveRange(expiredStates);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Cleaned up {Count} expired LTI states", expiredStates.Count);
            }
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private static string? ExtractReturnUrl(IEnumerable<System.Security.Claims.Claim> claims)
        {
            var launchPresentationClaim = claims
                .FirstOrDefault(c => c.Type == "https://purl.imsglobal.org/spec/lti/claim/launch_presentation")
                ?.Value;

            if (string.IsNullOrEmpty(launchPresentationClaim))
                return null;

            try
            {
                var doc = JsonDocument.Parse(launchPresentationClaim);
                if (doc.RootElement.TryGetProperty("return_url", out var returnUrlEl))
                    return returnUrlEl.GetString();
            }
            catch { /* malformed claim — ignore */ }

            return null;
        }

        private static LtiLaunchResult.LtiAgsClaimSet? ParseAgsEndpoint(IEnumerable<System.Security.Claims.Claim> claims)
        {
            var agsClaim = claims
                .FirstOrDefault(c => c.Type == "https://purl.imsglobal.org/spec/lti-ags/claim/endpoint")
                ?.Value;

            if (string.IsNullOrEmpty(agsClaim))
                return null;

            try
            {
                var doc = JsonDocument.Parse(agsClaim);
                var root = doc.RootElement;

                string[]? scopes = null;
                if (root.TryGetProperty("scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.Array)
                    scopes = scopeEl.EnumerateArray().Select(s => s.GetString()!).ToArray();

                string? lineItemUrl = root.TryGetProperty("lineitem", out var li) ? li.GetString() : null;
                string? lineItemsUrl = root.TryGetProperty("lineitems", out var lis) ? lis.GetString() : null;

                if (lineItemUrl == null && lineItemsUrl == null)
                    return null;

                return new LtiLaunchResult.LtiAgsClaimSet
                {
                    Scopes = scopes,
                    LineItemUrl = lineItemUrl,
                    LineItemsUrl = lineItemsUrl
                };
            }
            catch { return null; }
        }
    }
}
