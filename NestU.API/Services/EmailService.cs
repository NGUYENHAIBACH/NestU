using System.Net;
using System.Net.Mail;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetOtpAsync(string toEmail, string otp)
    {
        var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
        var port = _config.GetValue<int>("Smtp:Port", 587);
        var enableSsl = _config.GetValue<bool>("Smtp:EnableSsl", true);
        var userName = _config["Smtp:UserName"]?.Trim();
        var password = _config["Smtp:Password"]?.Replace(" ", "").Trim();
        var senderEmail = _config["Smtp:SenderEmail"]?.Trim();

        // Với Gmail SMTP, địa chỉ gửi (From) bắt buộc phải trùng với tài khoản xác thực (UserName)
        if (string.IsNullOrWhiteSpace(senderEmail) || (senderEmail.EndsWith("@nestu.vn") && !string.IsNullOrWhiteSpace(userName)))
        {
            senderEmail = userName;
        }

        var senderName = _config["Smtp:SenderName"] ?? "NestU Support";

        // Luôn ghi log OTP ở môi trường phát triển để dev/test thuận tiện ngay cả khi chưa cấu hình SMTP
        _logger.LogInformation("==================================================");
        _logger.LogInformation(" [NestU OTP RESET PASSWORD]");
        _logger.LogInformation(" Người nhận : {Email}", toEmail);
        _logger.LogInformation(" Mã OTP     : {Otp} (Hiệu lực trong 5 phút)", otp);
        _logger.LogInformation("==================================================");

        // Nếu chưa cấu hình tài khoản gửi mail thì dừng ở chế độ test (log ra console)
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Chưa cấu hình tài khoản Smtp:UserName / Smtp:Password trong appsettings.json. Hệ thống đã in mã OTP ở console/log.");
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password),
                Timeout = 10000 // 10 giây
            };

            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = $"[NestU] Mã OTP đặt lại mật khẩu: {otp}",
                IsBodyHtml = true,
                Body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
    <h2 style=""color: #0d6efd; margin-top: 0;"">Yêu cầu đặt lại mật khẩu</h2>
    <p>Xin chào,</p>
    <p>Hệ thống NestU nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{toEmail}</strong>.</p>
    <p>Mã xác thực (OTP) của bạn là:</p>
    <div style=""text-align: center; margin: 24px 0;"">
        <span style=""display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #212529; background: #f1f3f5; padding: 12px 24px; border-radius: 6px; border: 1px dashed #adb5bd;"">{otp}</span>
    </div>
    <p style=""color: #dc3545; font-weight: bold;"">⚠️ Mã này có hiệu lực trong vòng 5 phút.</p>
    <p style=""color: #6c757d; font-size: 13px;"">Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc đổi mật khẩu để bảo vệ tài khoản.</p>
    <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;"" />
    <p style=""color: #adb5bd; font-size: 12px; margin-bottom: 0;"">Đây là email tự động từ hệ thống NestU, vui lòng không trả lời thư này.</p>
</div>"
            };

            mail.To.Add(toEmail);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email OTP đến {Email}. Vui lòng kiểm tra cấu hình SMTP.", toEmail);
            // Không ngắt tiến trình để người kiểm thử vẫn có thể dùng mã OTP đã in trong log
        }
    }
}

