namespace NestU.API.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetOtpAsync(string toEmail, string otp);
}

