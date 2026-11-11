using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using PayOS;
using PayOS.Models.Webhooks;

namespace NestU.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentWebhookController : ControllerBase
{
    private readonly PayOSClient _payOS;
    private readonly NestUDbContext _db;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(PayOSClient payOS, NestUDbContext db, ILogger<PaymentWebhookController> logger)
    {
        _payOS = payOS;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Webhook nhận thông báo thanh toán tự động từ PayOS khi khách chuyển khoản thành công.
    /// </summary>
    [HttpPost("payos-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOSWebhook([FromBody] Webhook webhook)
    {
        try
        {
            // 1. Xác thực chữ ký số từ PayOS
            var data = await _payOS.Webhooks.VerifyAsync(webhook);
            _logger.LogInformation("PayOS Webhook nhận thanh toán: OrderCode={OrderCode}, Amount={Amount}, Desc={Desc}, Ref={Ref}",
                data.OrderCode, data.Amount, data.Description, data.Reference);

            var desc = (data.Description ?? "").ToUpper();
            var amount = (decimal)data.Amount;

            // 2. Nhận diện loại giao dịch qua Description (Ví dụ: "P102 T11 HD12" hoặc "COC P102 HD5")
            // A. Thanh toán tiền cọc (Deposit): Chứa "COC" hoặc "DEP"
            if (desc.Contains("COC") || desc.Contains("DEP"))
            {
                var match = Regex.Match(desc, @"(?:HD|DEP|COC)\D*(\d+)");
                if (!match.Success) match = Regex.Match(desc, @"\d+$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var contractId))
                {
                    var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == contractId);
                    if (contract != null && contract.DepositPaidAt == null)
                    {
                        contract.DepositPaidAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Đã tự động ghi nhận tiền cọc cho hợp đồng #{ContractId}", contractId);
                    }
                }
            }
            // B. Thanh toán hóa đơn (Invoice): Chứa "HD" hoặc "INV"
            else
            {
                var match = Regex.Match(desc, @"(?:HD|INV)\D*(\d+)");
                if (!match.Success) match = Regex.Match(desc, @"\d+$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var invoiceId))
                {
                    var inv = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == invoiceId);
                    if (inv != null && inv.Status != InvoiceStatus.Paid)
                    {
                        inv.Status = InvoiceStatus.Paid;
                        inv.RejectReason = null;
                        _db.Payments.Add(new Payment
                        {
                            InvoiceId = invoiceId,
                            Amount = amount,
                            ConfirmedAt = DateTime.UtcNow,
                            Note = $"Thanh toán tự động qua PayOS (Mã GD: {data.Reference})"
                        });
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Đã tự động gạch nợ hóa đơn #{InvoiceId} số tiền {Amount}", invoiceId, amount);
                    }
                }
            }

            return Ok(new { success = true, message = "Webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý PayOS webhook");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Xác nhận Webhook URL với PayOS (Dùng khi đăng ký Webhook URL với PayOS qua API).
    /// </summary>
    [HttpPost("confirm-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmWebhook([FromBody] ConfirmWebhookDto dto)
    {
        try
        {
            var res = await _payOS.Webhooks.ConfirmAsync(dto.WebhookUrl);
            return Ok(new { success = true, webhookUrl = res.WebhookUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class ConfirmWebhookDto
{
    public string WebhookUrl { get; set; } = string.Empty;
}
