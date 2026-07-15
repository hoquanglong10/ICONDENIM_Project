namespace ICONDENIM.Web.Configuration;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";
    public string Currency { get; set; } = "USD";
    public decimal VndPerUsd { get; set; } = 25000m;
    public int PendingOrderTimeoutMinutes { get; set; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !ClientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !ClientSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
