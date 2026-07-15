using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ICONDENIM.Web.Configuration;
using Microsoft.Extensions.Options;

namespace ICONDENIM.Web.Services;

public sealed class PayPalService
{
    private readonly HttpClient _httpClient;
    private readonly PayPalOptions _options;

    public PayPalService(HttpClient httpClient, IOptions<PayPalOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsConfigured => _options.IsConfigured;
    public string ClientId => _options.ClientId;
    public string Currency => string.IsNullOrWhiteSpace(_options.Currency) ? "USD" : _options.Currency.ToUpperInvariant();

    public async Task<PayPalCreateOrderResult> CreateOrderAsync(
        decimal amount,
        string referenceId,
        string description,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var value = amount.ToString("0.00", CultureInfo.InvariantCulture);

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId,
                    custom_id = referenceId,
                    description,
                    amount = new
                    {
                        currency_code = Currency,
                        value
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        shipping_preference = "NO_SHIPPING",
                        user_action = "PAY_NOW"
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/v2/checkout/orders"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", $"create-{referenceId}");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, "Không thể tạo PayPal Order");

        using var document = JsonDocument.Parse(body);
        var id = document.RootElement.GetProperty("id").GetString();
        var status = document.RootElement.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(id))
            throw new PayPalApiException("PayPal không trả về Order ID.", response.StatusCode, body);

        return new PayPalCreateOrderResult(id, status ?? "CREATED");
    }

    public async Task<PayPalCaptureOrderResult> CaptureOrderAsync(
        string paypalOrderId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUrl($"/v2/checkout/orders/{Uri.EscapeDataString(paypalOrderId)}/capture"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", $"capture-{paypalOrderId}");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, "Không thể capture PayPal Order");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var status = root.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString() ?? string.Empty
            : string.Empty;

        string? captureId = null;
        string? captureStatus = null;
        decimal? capturedAmount = null;
        string? currency = null;

        if (root.TryGetProperty("purchase_units", out var purchaseUnits) && purchaseUnits.GetArrayLength() > 0)
        {
            var purchaseUnit = purchaseUnits[0];
            if (purchaseUnit.TryGetProperty("payments", out var payments) &&
                payments.TryGetProperty("captures", out var captures) &&
                captures.GetArrayLength() > 0)
            {
                var capture = captures[0];
                captureId = capture.TryGetProperty("id", out var captureIdElement)
                    ? captureIdElement.GetString()
                    : null;
                captureStatus = capture.TryGetProperty("status", out var captureStatusElement)
                    ? captureStatusElement.GetString()
                    : null;

                if (capture.TryGetProperty("amount", out var amountElement))
                {
                    currency = amountElement.TryGetProperty("currency_code", out var currencyElement)
                        ? currencyElement.GetString()
                        : null;
                    if (amountElement.TryGetProperty("value", out var valueElement) &&
                        decimal.TryParse(valueElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                    {
                        capturedAmount = value;
                    }
                }
            }
        }

        return new PayPalCaptureOrderResult(
            paypalOrderId,
            status,
            captureId,
            captureStatus,
            capturedAmount,
            currency,
            body);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/v1/oauth2/token"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, "Không thể lấy PayPal access token");

        using var document = JsonDocument.Parse(body);
        var token = document.RootElement.TryGetProperty("access_token", out var tokenElement)
            ? tokenElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
            throw new PayPalApiException("PayPal không trả về access token.", response.StatusCode, body);

        return token;
    }

    private string BuildUrl(string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api-m.sandbox.paypal.com"
            : _options.BaseUrl.TrimEnd('/');
        return baseUrl + path;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "PayPal chưa được cấu hình. Hãy thêm PayPal:ClientId và PayPal:ClientSecret bằng User Secrets hoặc appsettings.Development.json.");
        }
    }

    private static void EnsureSuccess(HttpStatusCode statusCode, string body, string message)
    {
        if ((int)statusCode is >= 200 and <= 299) return;
        throw new PayPalApiException($"{message}. HTTP {(int)statusCode}.", statusCode, body);
    }
}

public sealed record PayPalCreateOrderResult(string Id, string Status);

public sealed record PayPalCaptureOrderResult(
    string OrderId,
    string Status,
    string? CaptureId,
    string? CaptureStatus,
    decimal? CapturedAmount,
    string? Currency,
    string RawResponse);

public sealed class PayPalApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public PayPalApiException(string message, HttpStatusCode statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
