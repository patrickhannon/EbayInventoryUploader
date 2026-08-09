using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarketplaceInventoryManager.Models;

namespace MarketplaceInventoryManager.Services;

public class WalmartApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WalmartApiConfig _config;
    private readonly SemaphoreSlim _tokenRefreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresUtc;

    public WalmartApiClient(HttpClient httpClient, WalmartApiConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public bool IsConfigured => _config.IsConfigured();

    public async Task<string> RequestBuyBoxReportAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_config.BaseUrl.TrimEnd('/')}/v3/reports/reportRequests?reportType=BUYBOX&reportVersion=v1");

        await AddMarketplaceHeadersAsync(request, cancellationToken);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Walmart report request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {content}");
        }

        using var document = JsonDocument.Parse(content);
        var requestId = FindString(document.RootElement, "requestId")
            ?? FindString(document.RootElement, "requestID")
            ?? FindString(document.RootElement, "id");

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new InvalidOperationException("Walmart report request succeeded but no requestId was returned.");
        }

        return requestId;
    }

    public async Task<string> WaitForReportReadyAsync(string requestId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= Math.Max(1, _config.MaxPollAttempts); attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_config.BaseUrl.TrimEnd('/')}/v3/reports/reportRequests/{Uri.EscapeDataString(requestId)}");

            await AddMarketplaceHeadersAsync(request, cancellationToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Walmart report status check failed: {(int)response.StatusCode} {response.ReasonPhrase} - {content}");
            }

            using var document = JsonDocument.Parse(content);
            var status = (FindString(document.RootElement, "status")
                ?? FindString(document.RootElement, "requestStatus")
                ?? string.Empty).Trim().ToUpperInvariant();

            if (status == "READY")
            {
                return requestId;
            }

            if (status == "ERROR" || status == "FAILED")
            {
                throw new InvalidOperationException($"Walmart report generation failed with status '{status}'.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _config.PollIntervalSeconds)), cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the Walmart Buy Box report to become ready.");
    }

    public async Task<string> DownloadBuyBoxReportAsync(string requestId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_config.BaseUrl.TrimEnd('/')}/v3/reports/downloadReport?requestId={Uri.EscapeDataString(requestId)}");

        await AddMarketplaceHeadersAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (IsRedirect(response) && response.Headers.Location != null)
        {
            var redirectUri = response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location
                : new Uri(new Uri(_config.BaseUrl.TrimEnd('/') + "/"), response.Headers.Location);

            ValidateReportDownloadUri(redirectUri);

            using var redirectedRequest = new HttpRequestMessage(HttpMethod.Get, redirectUri);
            using var redirectedResponse = await _httpClient.SendAsync(redirectedRequest, cancellationToken);
            var redirectedContent = await redirectedResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!redirectedResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Walmart redirected report download failed: {(int)redirectedResponse.StatusCode} {redirectedResponse.ReasonPhrase} - {redirectedContent}");
            }

            return redirectedContent;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Walmart report download failed: {(int)response.StatusCode} {response.ReasonPhrase} - {content}");
        }

        return content;
    }

    private void ValidateReportDownloadUri(Uri redirectUri)
    {
        if (!string.Equals(redirectUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Walmart report download redirect must use HTTPS.");
        }

        var marketplaceHost = new Uri(_config.BaseUrl).Host;
        var allowedHosts = new[]
        {
            marketplaceHost,
            "walmartapis.com",
            "walmart.com"
        }
        .Concat(_config.ReportDownloadAllowedHosts ?? [])
        .Where(host => !string.IsNullOrWhiteSpace(host))
        .Distinct(StringComparer.OrdinalIgnoreCase);

        if (!allowedHosts.Any(host => HostMatches(redirectUri.Host, host)))
        {
            throw new InvalidOperationException(
                $"Walmart report download redirect host '{redirectUri.Host}' is not in the allowed host list.");
        }
    }

    private async Task AddMarketplaceHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("WM_SEC.ACCESS_TOKEN", await GetAccessTokenAsync(cancellationToken));
        request.Headers.Add("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
        request.Headers.Add("WM_SVC.NAME", _config.ServiceName);
        request.Headers.Add("WM_CONSUMER.ID", _config.SellerId);
        request.Headers.Add("WM_CONSUMER.CHANNEL.TYPE", _config.ConsumerChannelType);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _accessTokenExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenRefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                _accessTokenExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl.TrimEnd('/')}/v3/token");
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
            request.Headers.Add("WM_SVC.NAME", _config.ServiceName);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Walmart token request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {content}");
            }

            using var document = JsonDocument.Parse(content);
            _accessToken = FindString(document.RootElement, "access_token")
                ?? throw new InvalidOperationException("Walmart token response did not include access_token.");

            var expiresInSeconds = FindInt(document.RootElement, "expires_in") ?? 900;
            _accessTokenExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

            return _accessToken;
        }
        finally
        {
            _tokenRefreshLock.Release();
        }
    }

    private static string? FindString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.ToString(),
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    _ => null
                };
            }

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FindString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static int? FindInt(JsonElement element, string propertyName)
    {
        var value = FindString(element, propertyName);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool IsRedirect(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        return statusCode is 301 or 302 or 303 or 307 or 308;
    }

    private static bool HostMatches(string actualHost, string allowedHost)
    {
        var normalizedAllowedHost = allowedHost.Trim().TrimStart('.');
        return actualHost.Equals(normalizedAllowedHost, StringComparison.OrdinalIgnoreCase) ||
               actualHost.EndsWith($".{normalizedAllowedHost}", StringComparison.OrdinalIgnoreCase);
    }
}
