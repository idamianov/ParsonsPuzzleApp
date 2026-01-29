using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    /// <summary>
    /// Singleton service for managing LTI RSA signing keys.
    /// Keys are loaded once at application startup for optimal performance.
    /// </summary>
    public class LtiKeyProvider : ILtiKeyProvider
    {
        private readonly RsaSecurityKey _signingKey;
        private readonly JsonWebKeySet _jwks;
        private readonly ILogger<LtiKeyProvider> _logger;

        public LtiKeyProvider(IOptions<LtiOptions> options, ILogger<LtiKeyProvider> logger)
        {
            _logger = logger;
            var ltiOptions = options.Value;

            _signingKey = LoadOrCreateSigningKey(ltiOptions);
            _jwks = CreateJwks(ltiOptions.KeyId);
        }

        public RsaSecurityKey GetSigningKey() => _signingKey;

        public JsonWebKeySet GetJwks() => _jwks;

        private RsaSecurityKey LoadOrCreateSigningKey(LtiOptions options)
        {
            RSA rsa;

            // Try to load from PEM string first
            if (!string.IsNullOrEmpty(options.PrivateKeyPem))
            {
                rsa = RSA.Create();
                rsa.ImportFromPem(options.PrivateKeyPem);
                _logger.LogInformation("Loaded RSA key from configuration");
            }
            // Try to load from file
            else if (!string.IsNullOrEmpty(options.PrivateKeyPath) && File.Exists(options.PrivateKeyPath))
            {
                var pem = File.ReadAllText(options.PrivateKeyPath);
                rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                _logger.LogInformation("Loaded RSA key from file: {Path}", options.PrivateKeyPath);
            }
            // Generate a new key
            else
            {
                rsa = RSA.Create(2048);
                _logger.LogWarning("Generated new RSA key. For production, configure a persistent key via Lti:PrivateKeyPath or Lti:PrivateKeyPem");

                // If a path is configured, save the generated key
                if (!string.IsNullOrEmpty(options.PrivateKeyPath))
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(options.PrivateKeyPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        var pem = rsa.ExportRSAPrivateKeyPem();
                        File.WriteAllText(options.PrivateKeyPath, pem);
                        _logger.LogInformation("Saved generated RSA key to: {Path}", options.PrivateKeyPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save generated RSA key");
                    }
                }
            }

            return new RsaSecurityKey(rsa) { KeyId = options.KeyId };
        }

        private JsonWebKeySet CreateJwks(string keyId)
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(_signingKey);
            jwk.Use = "sig";
            jwk.Alg = SecurityAlgorithms.RsaSha256;
            jwk.Kid = keyId;

            var jwks = new JsonWebKeySet();
            jwks.Keys.Add(jwk);
            return jwks;
        }
    }
}
