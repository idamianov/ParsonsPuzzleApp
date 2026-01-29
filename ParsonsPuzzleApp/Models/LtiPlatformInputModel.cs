using System.ComponentModel.DataAnnotations;

namespace ParsonsPuzzleApp.Models
{
    /// <summary>
    /// Shared input model for LTI Platform create and edit operations.
    /// </summary>
    public class LtiPlatformInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Platform name is required")]
        [Display(Name = "Platform Name")]
        [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issuer is required")]
        [Display(Name = "Issuer (iss)")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client ID is required")]
        [Display(Name = "Client ID")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Authorization endpoint is required")]
        [Display(Name = "Authorization Endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token endpoint is required")]
        [Display(Name = "Token Endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "JWKS URL is required")]
        [Display(Name = "JWKS URL")]
        public string JwksUrl { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Input model for LTI Deployment operations.
    /// </summary>
    public class LtiDeploymentInputModel
    {
        [Required(ErrorMessage = "Deployment ID is required")]
        [Display(Name = "Deployment ID")]
        public string DeploymentId { get; set; } = string.Empty;

        [Display(Name = "Name (optional)")]
        public string? Name { get; set; }

        [Display(Name = "Linked Bundle")]
        public int? BundleId { get; set; }
    }
}
