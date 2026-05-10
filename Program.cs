using Microsoft.Extensions.Configuration;
using EbayInventoryUploader.Models;
using EbayInventoryUploader.Services;

namespace EbayInventoryUploader;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== eBay Inventory Uploader ===\n");

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var ebayConfig = configuration.GetSection("EbayApi").Get<EbayApiConfig>();
        
        if (ebayConfig == null)
        {
            Console.WriteLine("Error: Unable to load eBay API configuration.");
            return;
        }

        // Validate configuration
        if (string.IsNullOrEmpty(ebayConfig.ApplicationId) || ebayConfig.ApplicationId.Contains("YOUR_"))
        {
            Console.WriteLine("⚠️  WARNING: You need to configure your eBay API credentials in appsettings.json");
            Console.WriteLine("\nTo get started:");
            Console.WriteLine("1. Go to https://developer.ebay.com/");
            Console.WriteLine("2. Create a developer account");
            Console.WriteLine("3. Get your App ID, Dev ID, and Cert ID");
            Console.WriteLine("4. Generate a user token");
            Console.WriteLine("5. Update the appsettings.json file with your credentials\n");
            Console.WriteLine("Press any key to continue with demo mode...");
            Console.ReadKey();
        }

        // Create HTTP client and API client
        using var httpClient = new HttpClient();
        var apiClient = new EbayApiClient(httpClient, ebayConfig);

        // Display menu
        while (true)
        {
            Console.WriteLine("\n--- Menu ---");
            Console.WriteLine("1. Upload single item");
            Console.WriteLine("2. Upload from CSV file");
            Console.WriteLine("3. Verify item before upload");
            Console.WriteLine("4. Test sample item");
            Console.WriteLine("5. Exit");
            Console.Write("\nSelect option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await UploadSingleItem(apiClient, ebayConfig);
                    break;
                case "2":
                    await UploadFromCsv(apiClient, ebayConfig);
                    break;
                case "3":
                    await VerifyItem(apiClient);
                    break;
                case "4":
                    await TestSampleItem(apiClient, ebayConfig);
                    break;
                case "5":
                    Console.WriteLine("\nGoodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    static async Task UploadSingleItem(EbayApiClient apiClient, EbayApiConfig ebayConfig)
    {
        Console.WriteLine("\n=== Upload Single Item ===");
        
        var item = new InventoryItem();

        // ...existing code...

        try
        {
            Console.WriteLine("\nUploading item to eBay...");
            var (success, itemId, errorMessage) = await apiClient.AddFixedPriceItemAsync(item);
            
            if (success && !string.IsNullOrEmpty(itemId))
            {
                Console.WriteLine($"✓ Success! Item ID: {itemId}");
                DisplayItemUrl(itemId, ebayConfig);
            }
            else
            {
                Console.WriteLine("✗ Failed to upload item.");
                if (!string.IsNullOrEmpty(errorMessage))
                    Console.WriteLine($"   Error: {errorMessage}");
                else
                    Console.WriteLine("   Check your API credentials and item details.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    static async Task UploadFromCsv(EbayApiClient apiClient, EbayApiConfig ebayConfig)
    {
        Console.WriteLine("\n=== Upload from CSV ===");
        Console.Write("Enter CSV file path: ");
        var filePath = Console.ReadLine();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                Console.WriteLine("CSV file is empty or invalid.");
                return;
            }

            // Skip header row
            var successCount = 0;
            var failCount = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 5)
                {
                    Console.WriteLine($"Line {i + 1}: Invalid format, skipping...");
                    failCount++;
                    continue;
                }

                var item = new InventoryItem
                {
                    Title = parts[0].Trim(),
                    Description = parts[1].Trim(),
                    StartPrice = decimal.TryParse(parts[2], out var p) ? p : 0,
                    Quantity = int.TryParse(parts[3], out var q) ? q : 1,
                    CategoryId = parts[4].Trim(),
                    SKU = parts.Length > 5 ? parts[5].Trim() : "",
                    Location = parts.Length > 6 ? parts[6].Trim() : "",
                    PostalCode = parts.Length > 7 ? parts[7].Trim() : ""
                };

                try
                {
                    Console.Write($"Uploading: {item.Title}... ");
                    var (success, itemId, errorMessage) = await apiClient.AddFixedPriceItemAsync(item);
                    
                    if (success && !string.IsNullOrEmpty(itemId))
                    {
                        Console.WriteLine($"✓ Success (ID: {itemId})");
                        var baseUrl = ebayConfig.UseSandbox ? "https://sandbox.ebay.com" : "https://www.ebay.com";
                        Console.WriteLine($"   View at: {baseUrl}/itm/{itemId}");
                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine("✗ Failed");
                        if (!string.IsNullOrEmpty(errorMessage))
                            Console.WriteLine($"   Error: {errorMessage}");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    failCount++;
                }

                // Add delay to avoid rate limiting
                await Task.Delay(1000);
            }

            Console.WriteLine($"\n--- Summary ---");
            Console.WriteLine($"Successful: {successCount}");
            Console.WriteLine($"Failed: {failCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CSV: {ex.Message}");
        }
    }

    static async Task VerifyItem(EbayApiClient apiClient)
    {
        Console.WriteLine("\n=== Verify Item ===");
        Console.WriteLine("This will validate your item without actually listing it.\n");

        var item = CreateSampleItem();
        
        Console.Write("Modify title? (press Enter to skip): ");
        var newTitle = Console.ReadLine();
        if (!string.IsNullOrEmpty(newTitle))
            item.Title = newTitle;

        try
        {
            Console.WriteLine("\nVerifying item...");
            var isValid = await apiClient.VerifyAddItemAsync(item);
            
            if (isValid)
            {
                Console.WriteLine("✓ Item validation successful! You can proceed with listing.");
            }
            else
            {
                Console.WriteLine("✗ Item validation failed. Check the item details.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    static async Task TestSampleItem(EbayApiClient apiClient, EbayApiConfig ebayConfig)
    {
        Console.WriteLine("\n=== Test Sample Item ===");
        
        var item = CreateSampleItem();
        
        Console.WriteLine("\nSample item details:");
        Console.WriteLine($"Title: {item.Title}");
        Console.WriteLine($"Price: ${item.StartPrice}");
        Console.WriteLine($"Quantity: {item.Quantity}");
        Console.WriteLine($"Category: {item.CategoryId}");
        
        Console.Write("\nProceed with upload? (y/n): ");
        if (Console.ReadLine()?.ToLower() != "y")
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        try
        {
            Console.WriteLine("\nUploading sample item...");
            var (success, itemId, errorMessage) = await apiClient.AddFixedPriceItemAsync(item);
            
            if (success && !string.IsNullOrEmpty(itemId))
            {
                Console.WriteLine($"✓ Success! Item ID: {itemId}");
                DisplayItemUrl(itemId, ebayConfig);
            }
            else
            {
                Console.WriteLine("✗ Failed to upload item.");
                if (!string.IsNullOrEmpty(errorMessage))
                    Console.WriteLine($"   Error: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    static void DisplayItemUrl(string itemId, EbayApiConfig config)
    {
        var baseUrl = config.UseSandbox ? "https://sandbox.ebay.com" : "https://www.ebay.com";
        var itemUrl = $"{baseUrl}/itm/{itemId}";
        
        Console.WriteLine($"\n📍 View your item at:");
        Console.WriteLine($"   {itemUrl}");
        
        Console.Write("\nOpen in browser? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            try
            {
                // Open URL in default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = itemUrl,
                    UseShellExecute = true
                });
                Console.WriteLine("✓ Opening in browser...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not open browser: {ex.Message}");
                Console.WriteLine($"   Please copy the URL above and paste it in your browser.");
            }
        }
    }

    static InventoryItem CreateSampleItem()
    {
        // Add timestamp to avoid duplicate listing errors
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        
        return new InventoryItem
        {
            Title = $"Sample Test Book - API Testing {timestamp}",
            Description = "This is a sample item for testing the eBay API integration. Test listing for development purposes.",
            StartPrice = 9.99m,
            Quantity = 1,
            CategoryId = "261186", // Books > Fiction & Literature - updated category
            Condition = "New",
            SKU = $"TEST-SKU-{timestamp}",
            Location = "New York",
            PostalCode = "10001",
            ShippingCost = 3.99m,
            ImageUrls = new List<string>(),
            ItemSpecifics = new Dictionary<string, string>
            {
                { "Author", "Test Author" },
                { "Book Title", $"Sample Test Book {timestamp}" },
                { "Language", "English" },
                { "Publication Year", "2024" },
                { "Format", "Paperback" }
            }
        };
    }
}
