namespace ArchiveSystem.Infrastructure.Integrations.MeliPayamak
{
    public interface IMeliPayamakService
    {
        Task<MeliPayamakSendResult> SendPatternAsync(MeliPayamakPatternRequest request, CancellationToken cancellationToken = default);
    }

    public sealed record MeliPayamakPatternRequest(
        string MobileNumber,
        string PatternCode,
        IReadOnlyList<string> Tokens);

    public sealed record MeliPayamakSendResult(
        bool Succeeded,
        string? ProviderTrackingId,
        string? ErrorMessage);
}
