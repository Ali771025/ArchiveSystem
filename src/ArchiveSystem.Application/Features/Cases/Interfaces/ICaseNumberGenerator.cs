namespace ArchiveSystem.Application.Features.Cases.Interfaces;

public interface ICaseNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
