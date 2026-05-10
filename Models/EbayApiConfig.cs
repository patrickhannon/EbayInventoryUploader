namespace EbayInventoryUploader.Models;

public class EbayApiConfig
{
    public string ApplicationId { get; set; } = string.Empty;
    public string CertId { get; set; } = string.Empty;
    public string DevId { get; set; } = string.Empty;
    public string UserToken { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string SandboxUrl { get; set; } = string.Empty;
    public bool UseSandbox { get; set; } = true;
    public string SiteId { get; set; } = "0";
    public string CompatibilityLevel { get; set; } = "1193";
    public string PayPalEmail { get; set; } = "paypal@example.com";
    
    public string GetEndpointUrl() => UseSandbox ? SandboxUrl : ApiUrl;
}
