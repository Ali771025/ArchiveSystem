namespace ArchiveSystem.Application.Features.AccessControl.Models
{
    public sealed record FileAccessDescriptor(long FileId, long? ClassificationId, IReadOnlyCollection<long> TagIds);
}
