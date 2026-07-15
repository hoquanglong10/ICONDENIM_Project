using System.ComponentModel.DataAnnotations;

namespace ICONDENIM.Web.ViewModels;

public sealed class PayPalCreateOrderRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    public string HoTenNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng.")]
    public string SdtNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    public string DiaChiNhan { get; set; } = string.Empty;

    public string? MaKhuyenMai { get; set; }
}

public sealed class PayPalCaptureOrderRequest
{
    [Range(1, int.MaxValue)]
    public int InternalOrderId { get; set; }

    [Required]
    public string PayPalOrderId { get; set; } = string.Empty;
}

public sealed class PayPalCancelOrderRequest
{
    [Range(1, int.MaxValue)]
    public int InternalOrderId { get; set; }

    [Required]
    public string PayPalOrderId { get; set; } = string.Empty;
}
