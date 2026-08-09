namespace EbayInventoryUploader.Models;

public class WalmartMonitorResult
{
    public string Sku { get; set; } = string.Empty;
    public string WalmartItemId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal CurrentShippingPrice { get; set; }
    public decimal BuyBoxPrice { get; set; }
    public decimal BuyBoxShippingPrice { get; set; }
    public bool OwnsBuyBox { get; set; }
    public decimal PriceGap { get; set; }
    public decimal? RecommendedPrice { get; set; }
    public string AlertState { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime LastCheckedUtc { get; set; }
}
