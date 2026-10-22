namespace ArchiveSystem.Presentation.Api.Models.Cases
{
    public sealed class MoveCaseRequest
    {
        public long? SourceClassificationId { get; set; }
        public long ClassificationId { get; set; }
    }
}
