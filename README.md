# eBay Inventory Uploader

A .NET 10 Core console application for uploading inventory items to eBay using the eBay Trading API.

## Features

- ✅ Upload single items to eBay
- ✅ Bulk upload from CSV files
- ✅ Verify items before listing
- ✅ Support for fixed price listings
- ✅ Configurable shipping and return policies
- ✅ Image upload support
- ✅ SKU and UPC tracking
- ✅ Sandbox mode for testing
- ✅ Walmart buy box monitoring via Buy Box reports

## Prerequisites

1. **.NET 10 SDK** installed
2. **eBay Developer Account** - Sign up at https://developer.ebay.com/
3. **API Credentials**: You'll need:
   - Application ID (App ID)
   - Developer ID (Dev ID)
   - Certificate ID (Cert ID)
   - User Token

## Setup Instructions

### 1. Get eBay API Credentials

1. Go to https://developer.ebay.com/
2. Sign in or create a developer account
3. Navigate to "My Account" → "Keys"
4. Create a new keyset (Sandbox or Production)
5. Note down your App ID, Dev ID, and Cert ID
6. Generate a User Token

### 2. Configure the Application

Edit `appsettings.json` and replace the placeholder values:

```json
{
  "EbayApi": {
    "ApplicationId": "YourActualAppId",
    "CertId": "YourActualCertId",
    "DevId": "YourActualDevId",
    "UserToken": "YourActualUserToken",
    "UseSandbox": true
  }
}
```

**Important:** Set `UseSandbox` to `true` for testing, and `false` for production.

### 3. Build and Run

```powershell
# Navigate to project directory
cd EbayInventoryUploader

# Build the project
dotnet build

# Run the application
dotnet run
```

## Usage

### Upload Single Item

1. Select option `1` from the menu
2. Enter item details when prompted:
   - Title
   - Description
   - Price
   - Quantity
   - Category ID (find at https://pages.ebay.com/sellerinformation/news/categorychanges.html)
   - SKU (optional)
   - Location
   - Postal Code

### Bulk Upload from CSV

1. Prepare a CSV file with the following format:

```csv
Title,Description,Price,Quantity,CategoryId,SKU,Location,PostalCode
Item 1,Description 1,29.99,10,11450,SKU001,New York,10001
Item 2,Description 2,39.99,5,11450,SKU002,Los Angeles,90001
```

2. Select option `2` from the menu
3. Enter the path to your CSV file
4. The application will upload each item with a 1-second delay between uploads

### Walmart Monitor Filter CSV

Create a CSV file with a single `Sku` or `WalmartItemId` column to limit the report to specific items:

```csv
Sku
WM-TEST-SKU-001
WM-TEST-SKU-002
```

### Verify Item Before Listing

1. Select option `3` from the menu
2. The application will validate your item configuration without actually creating a listing
3. Use this to test your API credentials and item details

### Monitor Walmart Buy Box

1. Configure the `WalmartApi` section in `appsettings.json`
2. Select option `5` from the menu
3. Optionally provide a CSV file containing SKUs or Walmart item IDs to filter against
4. The application will request a Walmart Buy Box report, wait for it to finish, and print price gaps and alerts
5. Optionally export the monitoring results to CSV

### Test with Sample Item

1. Select option `4` to upload a pre-configured sample item
2. Review the details and confirm to proceed
3. Useful for testing your API integration

## CSV File Format

Create a CSV file with these columns (in order):

| Column | Required | Description |
|--------|----------|-------------|
| Title | Yes | Item title (max 80 characters) |
| Description | Yes | Item description (HTML allowed) |
| Price | Yes | Starting price in USD |
| Quantity | Yes | Number of items available |
| CategoryId | Yes | eBay category ID |
| SKU | No | Your internal SKU |
| Location | No | Item location (city or region) |
| PostalCode | No | Postal/ZIP code |

## eBay Category IDs

Find the correct category ID for your items:
- Browse categories at: https://www.ebay.com/
- Use the eBay Category API
- Common categories:
  - 11450 - Computers/Tablets & Networking
  - 293 - Cell Phones & Accessories
  - 625 - Cameras & Photo
  - 58058 - Clothing, Shoes & Accessories

## Item Conditions

Available condition values:
- `New` (default)
- `New Other`
- `New with Defects`
- `Manufacturer Refurbished`
- `Seller Refurbished`
- `Used`
- `Very Good`
- `Good`
- `Acceptable`
- `For Parts or Not Working`

## Configuration Options

### Walmart Buy Box Monitoring

- `ClientId` / `ClientSecret` - Walmart Marketplace API credentials
- `PollIntervalSeconds` - How often to poll report status
- `MaxPollAttempts` - Maximum number of polling attempts before timeout
- `MinimumAllowedPrice` - Price floor for alerts and recommendations
- `MaximumPriceDropPercent` - Largest suggested drop allowed before the app suppresses a recommendation
- `RecommendPriceChanges` - When `true`, the app suggests a price to review; it never reprices automatically

The monitor uses Walmart's Buy Box report workflow:
1. Request a `BUYBOX` report
2. Poll until the report is ready
3. Download the CSV report
4. Evaluate whether you own the buy box and whether the buy box price is under your configured floor

### Shipping Services (US)

- `USPSPriority` - USPS Priority Mail
- `USPSPriorityMailInternational`
- `UPSGround`
- `FedExHomeDelivery`
- `ShippingMethodStandard`

### Return Policies

Configure in `InventoryItem.cs`:
- `ReturnWithinDays`: 14, 30, or 60 days
- `RefundOption`: MoneyBack, MoneyBackOrExchange, or MoneyBackOrReplacement
- `ShippingCostPaidBy`: Buyer or Seller

## Troubleshooting

### Authentication Errors

- Verify your API credentials are correct
- Ensure your User Token hasn't expired
- Make sure you're using the correct endpoint (Sandbox vs Production)

### Validation Errors

- Check category ID is valid
- Ensure required fields are filled
- Verify price and quantity are positive numbers
- Make sure title is under 80 characters

### Rate Limiting

- eBay has API call limits
- Add delays between bulk uploads (already implemented)
- Check your daily API call quota in the developer portal

## API Documentation

- eBay Trading API: https://developer.ebay.com/Devzone/XML/docs/Reference/eBay/index.html
- AddItem Call: https://developer.ebay.com/Devzone/XML/docs/Reference/eBay/AddItem.html
- AddFixedPriceItem: https://developer.ebay.com/Devzone/XML/docs/Reference/eBay/AddFixedPriceItem.html
- Walmart Buy Box report: https://developer.walmart.com/us-marketplace/docs/request-a-buy-box-insights-report
- Walmart access token: https://developer.walmart.com/us-marketplace/docs/get-an-access-token

## Project Structure

```
EbayInventoryUploader/
├── Program.cs                  # Main application entry point
├── appsettings.json            # Configuration file
├── sample-inventory.csv        # Sample CSV for bulk upload
├── sample-walmart-monitor.csv  # Sample CSV for Walmart buy box filters
├── Models/
│   ├── InventoryItem.cs        # Inventory item model
│   └── EbayApiConfig.cs        # API configuration model
└── Services/
    ├── EbayApiClient.cs        # eBay API client implementation
    ├── WalmartApiClient.cs     # Walmart API/report client implementation
    └── WalmartBuyBoxMonitorService.cs # Walmart buy box monitoring logic
```

## Security Notes

⚠️ **Important Security Considerations:**

1. **Never commit** `appsettings.json` with real credentials to source control
2. Use **environment variables** for production credentials
3. Keep your **User Token** secure and rotate it regularly
4. Use **Sandbox** mode for all testing
5. Consider storing credentials in **Azure Key Vault** or similar for production

## Environment Variables

Instead of using `appsettings.json`, you can set environment variables:

```powershell
$env:EbayApi__ApplicationId="YourAppId"
$env:EbayApi__CertId="YourCertId"
$env:EbayApi__DevId="YourDevId"
$env:EbayApi__UserToken="YourUserToken"
```

## License

This project is provided as-is for educational and commercial use.

## Support

For eBay API support:
- Developer Portal: https://developer.ebay.com/support
- Community Forums: https://community.ebay.com/t5/Developer-Network/ct-p/developer

## Changelog

### Version 1.0.0
- Initial release
- Single item upload
- Bulk CSV upload
- Item verification
- Sandbox support
