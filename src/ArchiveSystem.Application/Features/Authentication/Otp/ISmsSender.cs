namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default);
    }
}
