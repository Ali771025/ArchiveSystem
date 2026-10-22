using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Dossiers.Models;

public sealed class AttachDossierOwnerRequest
{
    public long? OwnerId { get; set; }
    public Guid? TaxpayerId { get; set; }
}

public sealed class DossierOwnerItemResponse
{
    public long OwnerId { get; set; }
    public OwnerType OwnerType { get; set; }
    public string NameOrTitle { get; set; } = string.Empty;
    public string NationalIdentifier { get; set; } = string.Empty;
    public string? TaxpayerId { get; set; }
    public string? Mobile { get; set; }
    public string? HomePhone { get; set; }
}

public sealed class DossierAvatarUploadResponse
{
    public string AvatarImagePath { get; set; } = string.Empty;
    public string ThumbnailImagePath { get; set; } = string.Empty;
}
