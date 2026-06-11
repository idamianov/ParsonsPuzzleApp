using Microsoft.IdentityModel.Tokens;

namespace ParsonsPuzzleApp.Interfaces
{
    /// <summary>
    /// Singleton service for managing LTI RSA signing keys.
    /// Keys are loaded once at application startup and reused throughout the application lifetime.
    /// </summary>
    public interface ILtiKeyProvider
    {
        /// <summary>
        /// Gets the RSA security key used for signing JWTs.
        /// </summary>
        RsaSecurityKey GetSigningKey();

        /// <summary>
        /// Gets the JSON Web Key Set containing the public key for platform verification.
        /// </summary>
        JsonWebKeySet GetJwks();
    }
}
