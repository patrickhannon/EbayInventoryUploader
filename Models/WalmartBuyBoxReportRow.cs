namespace MarketplaceInventoryManager.Models;

public class WalmartBuyBoxReportRow
{
    public string Sku { get; set; } = string.Empty;
    public string WalmartItemId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal SellerItemPrice { get; set; }
    public decimal SellerShippingPrice { get; set; }
    public decimal BuyBoxItemPrice { get; set; }
    public decimal BuyBoxShippingPrice { get; set; }
    public bool IsSellerBuyBoxWinner { get; set; }
}
