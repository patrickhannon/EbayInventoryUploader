using System.Globalization;
using System.Text;
using MarketplaceInventoryManager.Models;

namespace MarketplaceInventoryManager.Services;

public class WalmartBuyBoxMonitorService
{
    private readonly WalmartApiClient _apiClient;
    private readonly WalmartApiConfig _config;

    public WalmartBuyBoxMonitorService(WalmartApiClient apiClient, WalmartApiConfig config)
    {
        _apiClient = apiClient;
        _config = config;
    }

    public bool IsConfigured => _apiClient.IsConfigured;

    public async Task<List<WalmartMonitorResult>> MonitorAsync(IEnumerable<string>? skuFilter = null, CancellationToken cancellationToken = default)
    {
        var normalizedFilter = new HashSet<string>(
            (skuFilter ?? Enumerable.Empty<string>())
                .Where(sku => !string.IsNullOrWhiteSpace(sku))
                .Select(sku => sku.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var requestId = await _apiClient.RequestBuyBoxReportAsync(cancellationToken);
        await _apiClient.WaitForReportReadyAsync(requestId, cancellationToken);
        var csv = await _apiClient.DownloadBuyBoxReportAsync(requestId, cancellationToken);

        var rows = ParseBuyBoxReport(csv);
        if (normalizedFilter.Count > 0)
        {
            rows = rows
                .Where(row => normalizedFilter.Contains(row.Sku) || normalizedFilter.Contains(row.WalmartItemId))
                .ToList();
        }

        return rows
            .Select(EvaluateRow)
            .OrderBy(result => GetAlertPriority(result.AlertState))
            .ThenBy(result => result.AlertState, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.Sku, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> LoadSkuFilterFromCsv(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var values = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var fields = ParseCsvLine(lines[i]);
            if (fields.Count == 0)
            {
                continue;
            }

            if (i == 0 && IsHeader(fields[0]))
            {
                continue;
            }

            values.Add(fields[0].Trim());
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void ExportResultsToCsv(IEnumerable<WalmartMonitorResult> results, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();
        builder.AppendLine("Sku,WalmartItemId,ProductName,CurrentItemPrice,CurrentShippingPrice,BuyBoxItemPrice,BuyBoxShippingPrice,OwnsBuyBox,PriceGap,RecommendedPrice,AlertState,Recommendation,LastCheckedUtc");

        foreach (var result in results)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(result.Sku),
                EscapeCsv(result.WalmartItemId),
                EscapeCsv(result.ProductName),
                result.CurrentPrice.ToString("F2", CultureInfo.InvariantCulture),
                result.CurrentShippingPrice.ToString("F2", CultureInfo.InvariantCulture),
                result.BuyBoxPrice.ToString("F2", CultureInfo.InvariantCulture),
                result.BuyBoxShippingPrice.ToString("F2", CultureInfo.InvariantCulture),
                result.OwnsBuyBox ? "Yes" : "No",
                result.PriceGap.ToString("F2", CultureInfo.InvariantCulture),
                result.RecommendedPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeCsv(result.AlertState),
                EscapeCsv(result.Recommendation),
                result.LastCheckedUtc.ToString("O", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(filePath, builder.ToString());
    }

    private WalmartMonitorResult EvaluateRow(WalmartBuyBoxReportRow row)
    {
        var sellerLandedPrice = row.SellerItemPrice + row.SellerShippingPrice;
        var buyBoxLandedPrice = row.BuyBoxItemPrice + row.BuyBoxShippingPrice;
        var priceGap = sellerLandedPrice - buyBoxLandedPrice;

        var result = new WalmartMonitorResult
        {
            Sku = row.Sku,
            WalmartItemId = row.WalmartItemId,
            ProductName = row.ProductName,
            CurrentPrice = row.SellerItemPrice,
            CurrentShippingPrice = row.SellerShippingPrice,
            BuyBoxPrice = row.BuyBoxItemPrice,
            BuyBoxShippingPrice = row.BuyBoxShippingPrice,
            OwnsBuyBox = row.IsSellerBuyBoxWinner,
            PriceGap = priceGap,
            LastCheckedUtc = DateTime.UtcNow
        };

        if (row.IsSellerBuyBoxWinner)
        {
            result.AlertState = "Competitive";
            result.Recommendation = "You currently own the Walmart buy box.";
            return result;
        }

        if (buyBoxLandedPrice <= 0)
        {
            result.AlertState = "MissingData";
            result.Recommendation = "Buy box price was not available in the Walmart report.";
            return result;
        }

        if (buyBoxLandedPrice < _config.MinimumAllowedPrice)
        {
            result.AlertState = "BelowFloor";
            result.Recommendation = "Buy box price is below your configured minimum price floor.";
            return result;
        }

        var recommendedLandedPrice = buyBoxLandedPrice;
        var maximumDropAmount = sellerLandedPrice > 0
            ? sellerLandedPrice * (_config.MaximumPriceDropPercent / 100m)
            : decimal.MaxValue;
        var isPricedAboveBuyBox = sellerLandedPrice > buyBoxLandedPrice;
        var wouldExceedMaxDrop = sellerLandedPrice > 0 &&
                                 sellerLandedPrice - recommendedLandedPrice > maximumDropAmount;
        var recommendedItemPrice = Math.Max(0m, recommendedLandedPrice - row.SellerShippingPrice);

        result.RecommendedPrice = _config.RecommendPriceChanges && isPricedAboveBuyBox && !wouldExceedMaxDrop
            ? recommendedItemPrice
            : null;

        result.AlertState = "LostBuyBox";
        result.Recommendation = result.RecommendedPrice.HasValue
            ? $"Consider reviewing item price toward {result.RecommendedPrice.Value:F2} with current shipping unchanged."
            : isPricedAboveBuyBox
                ? "You lost the buy box; review price or fulfillment settings before repricing."
                : "You lost the buy box even though your landed price is already competitive; review fulfillment or listing quality.";

        return result;
    }

    private List<WalmartBuyBoxReportRow> ParseBuyBoxReport(string csv)
    {
        var rows = new List<WalmartBuyBoxReportRow>();
        using var reader = new StringReader(csv);

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return rows;
        }

        var headers = ParseCsvLine(headerLine);
        var headerIndex = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .GroupBy(entry => entry.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new WalmartBuyBoxReportRow
            {
                Sku = GetField(fields, headerIndex, "SKU"),
                WalmartItemId = GetField(fields, headerIndex, "Item ID"),
                ProductName = GetField(fields, headerIndex, "Product Name"),
                SellerItemPrice = ParseDecimal(GetField(fields, headerIndex, "Seller Item Price")),
                SellerShippingPrice = ParseDecimal(GetField(fields, headerIndex, "Seller Ship Price")),
                BuyBoxItemPrice = ParseDecimal(GetField(fields, headerIndex, "Buy Box Item Price")),
                BuyBoxShippingPrice = ParseDecimal(GetField(fields, headerIndex, "Buy Box Ship Price")),
                IsSellerBuyBoxWinner = ParseYesNo(GetField(fields, headerIndex, "isSellerBuyBoxWinner"))
            });
        }

        return rows;
    }

    private static string GetField(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> headerIndex, string header)
    {
        return headerIndex.TryGetValue(header, out var index) && index < fields.Count
            ? fields[index].Trim()
            : string.Empty;
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var normalized = value.Replace("$", string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }

    private static bool ParseYesNo(string value) =>
        value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static bool IsHeader(string value) =>
        value.Equals("sku", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("walmartitemid", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("itemid", StringComparison.OrdinalIgnoreCase);

    private static int GetAlertPriority(string alertState) => alertState switch
    {
        "BelowFloor" => 0,
        "LostBuyBox" => 1,
        "MissingData" => 2,
        "Competitive" => 3,
        _ => 4
    };

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];

            if (current == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (current == ',' && !inQuotes)
            {
                values.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(current);
        }

        values.Add(builder.ToString());
        return values;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
