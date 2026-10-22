namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface IRefreshTokenIssuer
    {
        Task<string> IssueAsync(long userId, CancellationToken cancellationToken = default);
    }
}
