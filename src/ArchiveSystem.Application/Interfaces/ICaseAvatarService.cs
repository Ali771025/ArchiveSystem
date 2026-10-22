using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Interfaces
{
    public interface ICaseAvatarService
    {
        Task<string> UploadAvatarAsync(long caseId, IFormFile file, CancellationToken cancellationToken = default);
    }
}
