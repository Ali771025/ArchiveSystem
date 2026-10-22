namespace ArchiveSystem.Application.Features.Classifications.Models
{
    public sealed class ClassificationTreeNodeDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long? ParentId { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsActive { get; set; }
        private int _directDossierCount;
        public int DossierCount
        {
            get => _directDossierCount + Children.Sum(child => child.DossierCount);
            set => _directDossierCount = value;
        }
        public int CaseCount => DossierCount;
        public string? SystemKey { get; set; }
        public List<ClassificationTreeNodeDto> Children { get; set; } = new();
    }

    public sealed class ClassificationSearchNodeDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long? ParentId { get; set; }
        public int DossierCount { get; set; }
        public string HierarchyPath { get; set; } = string.Empty;
    }

    public sealed class ClassificationMutationResultDto
    {
        public long Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public long? ParentId { get; init; }
    }
}
