using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ParsonsPuzzleApp.Constants;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Controllers
{
    /// <summary>
    /// Controller for handling LTI 1.3 launch flow endpoints.
    /// </summary>
    [ApiController]
    public class LtiController : ControllerBase
    {
        private readonly ILtiService _ltiService;
        private readonly IBundleAccessService _bundleAccessService;
        private readonly LtiOptions _options;
        private readonly ILogger<LtiController> _logger;

        public LtiController(
            ILtiService ltiService,
            IBundleAccessService bundleAccessService,
            IOptions<LtiOptions> options,
            ILogger<LtiController> logger)
        {
            _ltiService = ltiService;
            _bundleAccessService = bundleAccessService;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Returns the tool's JSON Web Key Set (public keys) for platform verification.
        /// Platforms use this to verify signatures on messages from this tool.
        /// </summary>
        [HttpGet("/.well-known/jwks.json")]
        [Produces("application/json")]
        public IActionResult GetJwks()
        {
            var jwks = _ltiService.GetJwks();

            // Serialize to proper JWKS format
            var jwksJson = new
            {
                keys = jwks.Keys.Select(k => new
                {
                    kty = k.Kty,
                    use = k.Use ?? "sig",
                    kid = k.Kid,
                    alg = k.Alg ?? "RS256",
                    n = k.N,
                    e = k.E
                })
            };

            return Ok(jwksJson);
        }

        /// <summary>
        /// OIDC Login Initiation endpoint.
        /// The platform redirects here to start the authentication flow.
        /// </summary>
        [HttpGet("lti/login")]
        [HttpPost("lti/login")]
        public async Task<IActionResult> Login(CancellationToken cancellationToken)
        {
            // Support both GET (query) and POST (form) - Moodle may use either
            var issuer = Request.Query["iss"].FirstOrDefault() ?? Request.Form["iss"].FirstOrDefault();
            var loginHint = Request.Query["login_hint"].FirstOrDefault() ?? Request.Form["login_hint"].FirstOrDefault();
            var targetLinkUri = Request.Query["target_link_uri"].FirstOrDefault() ?? Request.Form["target_link_uri"].FirstOrDefault();
            var ltiMessageHint = Request.Query["lti_message_hint"].FirstOrDefault() ?? Request.Form["lti_message_hint"].FirstOrDefault();
            var clientId = Request.Query["client_id"].FirstOrDefault() ?? Request.Form["client_id"].FirstOrDefault();

            _logger.LogInformation("LTI login initiation: issuer={Issuer}, client_id={ClientId}, target_link_uri={TargetLinkUri}",
                issuer, clientId, targetLinkUri);

            // Validate required parameters
            if (string.IsNullOrEmpty(issuer))
            {
                return BadRequest(new { error = "Missing required parameter: iss" });
            }

            if (string.IsNullOrEmpty(loginHint))
            {
                return BadRequest(new { error = "Missing required parameter: login_hint" });
            }

            // Find the platform registration
            // If client_id is not provided, we need to look up by issuer only
            var platform = clientId != null
                ? await _ltiService.GetPlatformAsync(issuer, clientId, cancellationToken)
                : null;

            if (platform == null)
            {
                _logger.LogWarning("Unknown platform: issuer={Issuer}, client_id={ClientId}", issuer, clientId);
                return BadRequest(new { error = "Unknown platform. Please register this platform first." });
            }

            // Create state for this login attempt
            var state = await _ltiService.CreateStateAsync(platform.ClientId, targetLinkUri, cancellationToken);

            // Build the authorization redirect URL
            var authUrl = BuildAuthorizationUrl(
                platform.AuthorizationEndpoint,
                platform.ClientId,
                _options.LaunchUrl,
                loginHint,
                state.State,
                state.Nonce,
                ltiMessageHint);

            _logger.LogDebug("Redirecting to platform authorization: {Url}", authUrl);

            return Redirect(authUrl);
        }

        /// <summary>
        /// LTI Launch endpoint.
        /// The platform redirects here after authentication with the ID token.
        /// </summary>
        [HttpPost("lti/launch")]
        public async Task<IActionResult> Launch(
            [FromForm] string? id_token,
            [FromForm] string? state,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("LTI launch received");

            if (string.IsNullOrEmpty(id_token))
            {
                return BadRequest(new { error = "Missing required parameter: id_token" });
            }

            if (string.IsNullOrEmpty(state))
            {
                return BadRequest(new { error = "Missing required parameter: state" });
            }

            // Validate and consume the state
            var ltiState = await _ltiService.ValidateAndConsumeStateAsync(state, cancellationToken);
            if (ltiState == null)
            {
                _logger.LogWarning("Invalid or expired state: {State}", state);
                return BadRequest(new { error = "Invalid or expired state. Please try launching again." });
            }

            // Validate the ID token
            var launchResult = await _ltiService.ValidateIdTokenAsync(id_token, ltiState.Nonce, cancellationToken);
            if (launchResult == null)
            {
                return BadRequest(new { error = "Invalid ID token. Authentication failed." });
            }

            // Get the platform
            var platform = await _ltiService.GetPlatformAsync(launchResult.Issuer, launchResult.ClientId, cancellationToken);
            if (platform == null)
            {
                return BadRequest(new { error = "Platform not found" });
            }

            // Find the deployment configuration
            var deployment = await _ltiService.GetDeploymentAsync(platform.Id, launchResult.DeploymentId, cancellationToken);

            _logger.LogInformation("LTI launch successful: user={UserId}, name={UserName}, roles={Roles}",
                launchResult.Subject, launchResult.DisplayName, string.Join(",", launchResult.Roles));

            // Determine where to redirect
            if (deployment?.BundleId != null)
            {
                // Grant access to the bundle and redirect to solve page
                var studentIdentifier = $"lti:{platform.Id}:{launchResult.Subject}";
                _bundleAccessService.GrantAccess(deployment.BundleId.Value, studentIdentifier);

                // Generate a new bundle attempt ID for this LTI session
                var bundleAttemptId = Guid.NewGuid();

                // Store LTI context in session for potential grade passback later
                HttpContext.Session.SetString(LtiSessionKeys.UserId, launchResult.Subject);
                HttpContext.Session.SetString(LtiSessionKeys.UserName, launchResult.DisplayName);
                HttpContext.Session.SetString(LtiSessionKeys.PlatformId, platform.Id.ToString());
                HttpContext.Session.SetString(LtiSessionKeys.DeploymentId, launchResult.DeploymentId);
                HttpContext.Session.SetString(LtiSessionKeys.BundleAttemptId, bundleAttemptId.ToString());

                if (!string.IsNullOrEmpty(launchResult.ResourceLinkId))
                {
                    HttpContext.Session.SetString(LtiSessionKeys.ResourceLinkId, launchResult.ResourceLinkId);
                }

                // Redirect to the bundle (puzzleIndex is 1-based)
                return RedirectToPage("/SolvePuzzle", new
                {
                    bundleId = deployment.BundleId.Value,
                    studentId = studentIdentifier,
                    puzzleIndex = 1,
                    bundleAttemptId = bundleAttemptId
                });
            }
            else
            {
                // No deployment configured - show a setup page or error
                if (launchResult.IsInstructor)
                {
                    // Store deployment info in session for setup
                    HttpContext.Session.SetString(LtiSessionKeys.SetupPlatformId, platform.Id.ToString());
                    HttpContext.Session.SetString(LtiSessionKeys.SetupDeploymentId, launchResult.DeploymentId);
                    HttpContext.Session.SetString(LtiSessionKeys.SetupMessage,
                        $"This deployment ({launchResult.DeploymentId}) is not configured. Please link it to a bundle.");

                    return RedirectToPage("/Instructor/Bundles");
                }
                else
                {
                    return BadRequest(new { error = "This LTI deployment has not been configured. Please contact your instructor." });
                }
            }
        }

        private static string BuildAuthorizationUrl(
            string authEndpoint,
            string clientId,
            string redirectUri,
            string loginHint,
            string state,
            string nonce,
            string? ltiMessageHint)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["scope"] = "openid";
            query["response_type"] = "id_token";
            query["response_mode"] = "form_post";
            query["prompt"] = "none";
            query["client_id"] = clientId;
            query["redirect_uri"] = redirectUri;
            query["login_hint"] = loginHint;
            query["state"] = state;
            query["nonce"] = nonce;

            if (!string.IsNullOrEmpty(ltiMessageHint))
            {
                query["lti_message_hint"] = ltiMessageHint;
            }

            return $"{authEndpoint}?{query}";
        }
    }
}
