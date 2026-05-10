# Quick Start Guide

## 1. Get Your eBay API Credentials

Before you can use this application, you need eBay API credentials:

### Step-by-Step:

1. **Visit eBay Developers**
   - Go to: https://developer.ebay.com/
   - Click "Register" or "Sign In"

2. **Create a Developer Account**
   - Use your existing eBay account or create a new one
   - Accept the developer terms

3. **Generate Keyset**
   - Go to: https://developer.ebay.com/my/keys
   - Click "Get a Sandbox Keyset" (for testing)
   - Note down:
     - App ID (Client ID)
     - Dev ID
     - Cert ID (Client Secret)

4. **Generate User Token**
   - On the same page, click "Get a User Token"
   - Sign in with your Sandbox eBay account
   - Grant permissions
   - Copy the token (it's long!)

## 2. Configure the Application

Open `appsettings.json` and update:

```json
{
  "EbayApi": {
    "ApplicationId": "YourAppIdHere-Sandbo-PRD-1234567890-12345678",
    "CertId": "PRD-1234567890123-45678901-2345-6789-0123-4567",
    "DevId": "12345678-9012-3456-7890-123456789012",
    "UserToken": "v^1.1#i^1#p^3#r^1#f^0#I^3#t^Ul4xMF8...(very long token)..."
  }
}
```

## 3. Run the Application

```powershell
dotnet run
```

## 4. Quick Test

1. Select option **4** (Test sample item)
2. Review the sample item details
3. Type `y` to upload
4. Check the result!

## 5. Upload Your Inventory

### Option A: Single Item
- Select option **1**
- Enter item details when prompted

### Option B: Bulk Upload from CSV
- Create a CSV file (see `sample-inventory.csv`)
- Select option **2**
- Enter the CSV file path
- All items will be uploaded with 1-second delays

## CSV Template

```csv
Title,Description,Price,Quantity,CategoryId,SKU,Location,PostalCode
Wireless Mouse,High quality wireless mouse,15.99,100,80053,WM-001,New York,10001
USB Cable,6ft USB-C Cable,9.99,200,11450,USB-C-6FT,Chicago,60601
```

## Finding Category IDs

Common categories:
- **80053** - Computer Components & Parts > I/O Devices > Mice & Trackballs
- **11450** - Computers/Tablets & Networking
- **175673** - Cell Phone Accessories
- **625** - Cameras & Photo
- **293** - Cell Phones & Smartphones

Or browse: https://www.ebay.com/

## Tips

✅ **Always test in Sandbox first** - Set `UseSandbox: true` in appsettings.json

✅ **Use VerifyAddItem** - Select option 3 to validate without listing

✅ **Keep tokens secure** - Never share or commit credentials to Git

✅ **Check item policies** - Different categories have different requirements

✅ **Add good descriptions** - Better descriptions = better sales

## Troubleshooting

### "Authentication failed"
- Double-check your App ID, Dev ID, Cert ID
- Verify your User Token hasn't expired
- Ensure you're using Sandbox credentials with Sandbox endpoint

### "Invalid category"
- Verify the category ID exists
- Some categories require additional attributes
- Check eBay's category structure

### "Missing required field"
- Ensure Title, Description, Price, Quantity, CategoryId are all filled
- Location and PostalCode are recommended

## Next Steps

1. Test in **Sandbox** environment
2. When ready, switch to **Production**:
   - Get Production keys
   - Update appsettings.json
   - Set `UseSandbox: false`
3. Start listing your real inventory!

## Support

- eBay Developer Portal: https://developer.ebay.com/support
- Trading API Docs: https://developer.ebay.com/Devzone/XML/docs/Reference/eBay/
- Community: https://community.ebay.com/t5/Developer-Network/ct-p/developer

---

**Happy Selling! 🎉**
