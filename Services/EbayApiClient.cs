using System.Text;
using System.Xml.Linq;
using EbayInventoryUploader.Models;

namespace EbayInventoryUploader.Services;

public class EbayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly EbayApiConfig _config;

    public EbayApiClient(HttpClient httpClient, EbayApiConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<(bool success, string itemId, string errorMessage)> AddItemAsync(InventoryItem item)
    {
        var xmlRequest = BuildAddItemRequest(item);
        var response = await SendRequestAsync("AddItem", xmlRequest);
        
        // Parse response and extract ItemID
        var itemId = ExtractItemIdFromResponse(response);
        var errorMsg = ExtractErrorMessage(response);
        var success = CheckResponseSuccess(response);
        
        return (success, itemId, errorMsg);
    }

    public async Task<(bool success, string itemId, string errorMessage)> AddFixedPriceItemAsync(InventoryItem item)
    {
        // Use AddItem instead of AddFixedPriceItem to avoid error 21843 in sandbox
        var xmlRequest = BuildAddItemRequest(item);
        var response = await SendRequestAsync("AddItem", xmlRequest);
        
        var itemId = ExtractItemIdFromResponse(response);
        var errorMsg = ExtractErrorMessage(response);
        var success = CheckResponseSuccess(response);
        
        return (success, itemId, errorMsg);
    }

    public async Task<bool> VerifyAddItemAsync(InventoryItem item)
    {
        var xmlRequest = BuildAddItemRequest(item);
        var response = await SendRequestAsync("VerifyAddItem", xmlRequest);
        
        // Check if verification succeeded
        return CheckResponseSuccess(response);
    }

    private async Task<string> SendRequestAsync(string callName, string xmlBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetEndpointUrl());
        
        // Add required headers
        request.Headers.Add("X-EBAY-API-SITEID", _config.SiteId);
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", _config.CompatibilityLevel);
        request.Headers.Add("X-EBAY-API-CALL-NAME", callName);
        request.Headers.Add("X-EBAY-API-APP-NAME", _config.ApplicationId);
        request.Headers.Add("X-EBAY-API-DEV-NAME", _config.DevId);
        request.Headers.Add("X-EBAY-API-CERT-NAME", _config.CertId);
        
        request.Content = new StringContent(xmlBody, Encoding.UTF8, "text/xml");
        
        // Debug logging
        Console.WriteLine($"\n[DEBUG] Request URL: {_config.GetEndpointUrl()}");
        Console.WriteLine($"[DEBUG] Request XML:\n{xmlBody}\n");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[DEBUG] Response XML:\n{responseContent}\n");
        
        return responseContent;
    }

    private string BuildAddItemRequest(InventoryItem item)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<AddItemRequest xmlns=\"urn:ebay:apis:eBLBaseComponents\">");
        xml.AppendLine($"  <RequesterCredentials>");
        xml.AppendLine($"    <eBayAuthToken>{_config.UserToken}</eBayAuthToken>");
        xml.AppendLine($"  </RequesterCredentials>");
        xml.AppendLine("  <ErrorLanguage>en_US</ErrorLanguage>");
        xml.AppendLine("  <WarningLevel>High</WarningLevel>");
        xml.AppendLine($"  <Item>");
        xml.AppendLine($"    <Title>{EscapeXml(item.Title)}</Title>");
        xml.AppendLine($"    <Description><![CDATA[{item.Description}]]></Description>");
        xml.AppendLine($"    <PrimaryCategory>");
        xml.AppendLine($"      <CategoryID>{item.CategoryId}</CategoryID>");
        xml.AppendLine($"    </PrimaryCategory>");
        xml.AppendLine($"    <StartPrice>{item.StartPrice:F2}</StartPrice>");
        xml.AppendLine($"    <Quantity>{item.Quantity}</Quantity>");
        xml.AppendLine($"    <ConditionID>{GetConditionId(item.Condition)}</ConditionID>");
        xml.AppendLine($"    <Country>{item.Country}</Country>");
        xml.AppendLine($"    <Currency>{item.Currency}</Currency>");
        xml.AppendLine($"    <Location>{EscapeXml(item.Location)}</Location>");
        xml.AppendLine($"    <PostalCode>{item.PostalCode}</PostalCode>");
        xml.AppendLine($"    <ListingType>{item.ListingType}</ListingType>");
        xml.AppendLine($"    <ListingDuration>GTC</ListingDuration>");
        
        if (!string.IsNullOrEmpty(item.SKU))
        {
            xml.AppendLine($"    <SKU>{EscapeXml(item.SKU)}</SKU>");
        }
        
        // Add Item Specifics (required for many categories)
        if (item.ItemSpecifics.Any())
        {
            xml.AppendLine($"    <ItemSpecifics>");
            foreach (var specific in item.ItemSpecifics)
            {
                xml.AppendLine($"      <NameValueList>");
                xml.AppendLine($"        <Name>{EscapeXml(specific.Key)}</Name>");
                xml.AppendLine($"        <Value>{EscapeXml(specific.Value)}</Value>");
                xml.AppendLine($"      </NameValueList>");
            }
            xml.AppendLine($"    </ItemSpecifics>");
        }
        
        // Add images
        if (item.ImageUrls.Any())
        {
            xml.AppendLine($"    <PictureDetails>");
            foreach (var imageUrl in item.ImageUrls)
            {
                xml.AppendLine($"      <PictureURL>{EscapeXml(imageUrl)}</PictureURL>");
            }
            xml.AppendLine($"    </PictureDetails>");
        }
        
        // Shipping details
        xml.AppendLine($"    <ShippingDetails>");
        xml.AppendLine($"      <ShippingType>Flat</ShippingType>");
        xml.AppendLine($"      <ShippingServiceOptions>");
        xml.AppendLine($"        <ShippingService>{item.ShippingService}</ShippingService>");
        xml.AppendLine($"        <ShippingServiceCost>{item.ShippingCost:F2}</ShippingServiceCost>");
        xml.AppendLine($"        <ShippingServicePriority>1</ShippingServicePriority>");
        xml.AppendLine($"      </ShippingServiceOptions>");
        xml.AppendLine($"    </ShippingDetails>");
        xml.AppendLine($"    <DispatchTimeMax>3</DispatchTimeMax>");
        
        // Return policy
        xml.AppendLine($"    <ReturnPolicy>");
        xml.AppendLine($"      <ReturnsAcceptedOption>{item.ReturnPolicy}</ReturnsAcceptedOption>");
        xml.AppendLine($"      <ReturnsWithinOption>Days_{item.ReturnWithinDays}</ReturnsWithinOption>");
        xml.AppendLine($"      <RefundOption>{item.RefundOption}</RefundOption>");
        xml.AppendLine($"      <ShippingCostPaidByOption>{item.ShippingCostPaidBy}</ShippingCostPaidByOption>");
        xml.AppendLine($"    </ReturnPolicy>");
        
        // Payment methods - Sandbox uses managed payments (no explicit payment methods needed for modern API)
        // Removing payment method tags to avoid sandbox compatibility issues
        
        xml.AppendLine($"  </Item>");
        xml.AppendLine("</AddItemRequest>");
        
        return xml.ToString();
    }

    private string BuildAddFixedPriceItemRequest(InventoryItem item)
    {
        // Similar to BuildAddItemRequest but optimized for fixed price items
        return BuildAddItemRequest(item);
    }

    private string ExtractItemIdFromResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            if (doc.Root == null)
            {
                return string.Empty;
            }

            var ns = doc.Root.GetDefaultNamespace();
            var itemId = doc.Root?.Element(ns + "ItemID")?.Value ?? string.Empty;
            return itemId;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractErrorMessage(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            if (doc.Root == null)
            {
                return string.Empty;
            }

            var ns = doc.Root.GetDefaultNamespace();
            
            var errors = doc.Root?.Element(ns + "Errors");
            if (errors != null)
            {
                var shortMsg = errors.Element(ns + "ShortMessage")?.Value ?? "";
                var longMsg = errors.Element(ns + "LongMessage")?.Value ?? "";
                var errorCode = errors.Element(ns + "ErrorCode")?.Value ?? "";
                
                if (!string.IsNullOrEmpty(longMsg))
                    return $"[{errorCode}] {longMsg}";
                if (!string.IsNullOrEmpty(shortMsg))
                    return $"[{errorCode}] {shortMsg}";
            }
            
            return string.Empty;
        }
        catch
        {
            return "Unable to parse error response";
        }
    }

    private bool CheckResponseSuccess(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            if (doc.Root == null)
            {
                return false;
            }

            var ns = doc.Root.GetDefaultNamespace();
            var ack = doc.Root?.Element(ns + "Ack")?.Value ?? string.Empty;
            return ack == "Success" || ack == "Warning";
        }
        catch
        {
            return false;
        }
    }

    private string GetConditionId(string condition)
    {
        return condition.ToLower() switch
        {
            "new" => "1000",
            "new other" => "1500",
            "new with defects" => "1750",
            "manufacturer refurbished" => "2000",
            "seller refurbished" => "2500",
            "used" => "3000",
            "very good" => "4000",
            "good" => "5000",
            "acceptable" => "6000",
            "for parts or not working" => "7000",
            _ => "1000"
        };
    }

    private string EscapeXml(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
