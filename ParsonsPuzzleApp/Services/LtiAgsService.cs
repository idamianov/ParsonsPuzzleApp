using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;

namespace ParsonsPuzzleApp.Services
{
    /// <summary>
    /// Handles OAuth 2.0 token exchange and grade passback to the LMS via LTI AGS.
    /// </summary>
    public class LtiAgsService : ILtiAgsService
    {
        private const string AgsScoreScope = "https://purl.imsglobal.org/spec/lti-ags/scope/score";

        private readonly ApplicationDbContext _context;
        private readonly ILtiKeyProvider _keyProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LtiAgsService> _logger;

        public LtiAgsService(
            ApplicationDbContext context,
            ILtiKeyProvider keyProvider,
            IHttpClientFactory httpClientFactory,
            ILogger<LtiAgsService> logger)
        {
            _context = context;
            _keyProvider = keyProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendGradeAsync(int ltiSessionId, CancellationToken ct = default)
        {
            var session = await _context.LtiSessions
                .Include(s => s.Platform)
                .Include(s => s.Bundle)
                .FirstOrDefaultAsync(s => s.Id == ltiSessionId, ct);

            if (session == null)
            {
                _logger.LogWarning("LTI session {SessionId} not found for grade passback", ltiSessionId);
                return false;
            }

            if (session.GradeSent)
            {
                _logger.LogDebug("Grade already sent for session {SessionId}", ltiSessionId);
                return true;
            }

            // Find the resource link to get the score URL
            var resourceLink = await _context.LtiResourceLinks
                .FirstOrDefaultAsync(rl =>
                    rl.LtiPlatformId == session.LtiPlatformId &&
                    rl.ResourceLinkId == session.ResourceLinkId, ct);

            if (resourceLink?.LineItemUrl == null)
            {
                _logger.LogWarning("No line item URL found for session {SessionId} (resource link: {ResourceLinkId})",
                    ltiSessionId, session.ResourceLinkId);
                return false;
            }

            var grade = await CalculateGradeAsync(session.BundleAttemptId, ct);

            var accessToken = await GetAccessTokenAsync(session.Platform, AgsScoreScope, ct);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to obtain access token for grade passback (session {SessionId})", ltiSessionId);
                return false;
            }

            // Insert /scores before any query string in the LineItemUrl
            var lineItemUri = new Uri(resourceLink.LineItemUrl);
            var scoreUrl = lineItemUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/scores" + lineItemUri.Query;

            var scorePayload = new
            {
                userId = session.UserId ?? string.Empty,
                scoreGiven = grade,
                scoreMaximum = 100.0,
                activityProgress = "Completed",
                gradingProgress = "FullyGraded",
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var json = JsonSerializer.Serialize(scorePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.ims.lis.v1.score+json");

            var httpClient = _httpClientFactory.CreateClient("LtiPlatform");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, scoreUrl) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    session.GradeSent = true;
                    session.GradeSentAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(ct);

                    _logger.LogInformation("Grade {Grade}% sent for session {SessionId}", grade, ltiSessionId);
                    return true;
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("AGS score POST failed for session {SessionId}: {Status} {Body}",
                        ltiSessionId, response.StatusCode, body);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending grade for session {SessionId}", ltiSessionId);
                return false;
            }
        }

        public async Task<string?> GetAccessTokenAsync(LtiPlatform platform, string scope, CancellationToken ct = default)
        {
            // Check cache
            var cachedToken = await _context.LtiAccessTokens
                .FirstOrDefaultAsync(t =>
                    t.LtiPlatformId == platform.Id &&
                    t.Scope == scope &&
                    t.ExpiresAt > DateTime.UtcNow.AddSeconds(30), ct);

            if (cachedToken != null)
            {
                return cachedToken.AccessToken;
            }

            // Request new token via OAuth 2.0 client credentials + JWT assertion
            var clientAssertion = BuildClientAssertion(platform);

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                ["client_assertion"] = clientAssertion,
                ["scope"] = scope
            };

            var httpClient = _httpClientFactory.CreateClient("LtiPlatform");

            try
            {
                var response = await httpClient.PostAsync(
                    platform.TokenEndpoint,
                    new FormUrlEncodedContent(tokenRequest),
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Token request failed for platform {PlatformId}: {Status} {Body}",
                        platform.Id, response.StatusCode, body);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseJson);

                if (tokenResponse == null ||
                    !tokenResponse.TryGetValue("access_token", out var tokenElement) ||
                    !tokenResponse.TryGetValue("expires_in", out var expiresElement))
                {
                    _logger.LogError("Unexpected token response format for platform {PlatformId}", platform.Id);
                    return null;
                }

                var accessToken = tokenElement.GetString();
                if (string.IsNullOrEmpty(accessToken)) return null;

                var expiresInSeconds = expiresElement.GetInt32();
                var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);

                // Evict any expired tokens for this platform+scope and cache the new one
                var oldTokens = await _context.LtiAccessTokens
                    .Where(t => t.LtiPlatformId == platform.Id && t.Scope == scope)
                    .ToListAsync(ct);
                _context.LtiAccessTokens.RemoveRange(oldTokens);

                _context.LtiAccessTokens.Add(new LtiAccessToken
                {
                    LtiPlatformId = platform.Id,
                    Scope = scope,
                    AccessToken = accessToken,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(ct);

                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception requesting access token for platform {PlatformId}", platform.Id);
                return null;
            }
        }

        private string BuildClientAssertion(LtiPlatform platform)
        {
            var signingKey = _keyProvider.GetSigningKey();
            var now = DateTimeOffset.UtcNow;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Iss, platform.ClientId),
                new Claim(JwtRegisteredClaimNames.Sub, platform.ClientId),
                new Claim(JwtRegisteredClaimNames.Aud, platform.TokenEndpoint),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, now.AddMinutes(5).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<double> CalculateGradeAsync(Guid bundleAttemptId, CancellationToken ct)
        {
            var attempts = await _context.StudentAttempts
                .Where(a => a.BundleAttemptId == bundleAttemptId)
                .ToListAsync(ct);

            var puzzleIds = attempts.Select(a => a.PuzzleId).Distinct().ToList();
            var totalPuzzles = puzzleIds.Count;
            var correctPuzzles = puzzleIds.Count(pid =>
                attempts.Any(a => a.PuzzleId == pid && a.IsCorrect));

            return totalPuzzles > 0 ? (double)correctPuzzles / totalPuzzles * 100 : 0;
        }
    }
}
