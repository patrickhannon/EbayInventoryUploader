namespace EbayInventoryUploader.Models;

public class WalmartApiConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://marketplace.walmartapis.com";
    public string ServiceName { get; set; } = "Walmart Marketplace";
    public string SellerId { get; set; } = string.Empty;
    public string ConsumerChannelType { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxPollAttempts { get; set; } = 24;
    public decimal MinimumAllowedPrice { get; set; } = 0m;
    public decimal MaximumPriceDropPercent { get; set; } = 20m;
    public bool RecommendPriceChanges { get; set; } = false;
    public string AlertWebhookUrl { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(SellerId) &&
        !string.IsNullOrWhiteSpace(ConsumerChannelType) &&
        !ClientId.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !ClientSecret.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !SellerId.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !ConsumerChannelType.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
}
