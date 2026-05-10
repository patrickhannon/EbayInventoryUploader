namespace EbayInventoryUploader.Models;

public class InventoryItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartPrice { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = "New";
    public int Duration { get; set; } = 7; // Days (GTC = Good 'Til Cancelled = 30)
    public string ListingType { get; set; } = "FixedPriceItem"; // or "Chinese" for auction
    public string Currency { get; set; } = "USD";
    public string Country { get; set; } = "US";
    public string Location { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string SKU { get; set; } = string.Empty;
    public string UPC { get; set; } = string.Empty;
    
    // Item Specifics (category-dependent attributes like Author, Brand, etc.)
    public Dictionary<string, string> ItemSpecifics { get; set; } = new();
    
    // Shipping details
    public string ShippingService { get; set; } = "USPSPriority";
    public decimal ShippingCost { get; set; }
    public List<string> ShipToLocations { get; set; } = new() { "US" };
    
    // Return policy
    public string ReturnPolicy { get; set; } = "ReturnsAccepted";
    public int ReturnWithinDays { get; set; } = 30;
    public string RefundOption { get; set; } = "MoneyBack";
    public string ShippingCostPaidBy { get; set; } = "Buyer";
}
