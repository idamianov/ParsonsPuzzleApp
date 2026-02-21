using System.ComponentModel.DataAnnotations;

namespace ParsonsPuzzleApp.Models
{
    /// <summary>
    /// Общ входен модел за създаване и редактиране на LTI Платформа.
    /// </summary>
    public class LtiPlatformInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на платформата е задължително")]
        [Display(Name = "Име на платформата")]
        [StringLength(200, ErrorMessage = "Името трябва да е по-малко от 200 символа")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issuer (издател) е задължителен")]
        [Display(Name = "Issuer (iss) (издател)")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client ID е задължителен")]
        [Display(Name = "Client ID (клиентски индентификатор)")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Крайната точка за авторизация е задължителна")]
        [Display(Name = "Authorization endpoint (kрайна точка за авторизация)")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Крайната точка за токени е задължителна")]
        [Display(Name = "Token endpoint (kрайна точка за токени)")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "JWKS URL е задължителен")]
        [Display(Name = "JWKS URL")]
        public string JwksUrl { get; set; } = string.Empty;

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Входен модел за LTI Deployment операции.
    /// </summary>
    public class LtiDeploymentInputModel
    {
        [Required(ErrorMessage = "Deployment ID е задължителен")]
        [Display(Name = "Deployment ID")]
        public string DeploymentId { get; set; } = string.Empty;

        [Display(Name = "Име (незадължително)")]
        public string? Name { get; set; }

        [Display(Name = "Bundle ID (свързан пакет)")]
        public int? BundleId { get; set; }
    }
}
