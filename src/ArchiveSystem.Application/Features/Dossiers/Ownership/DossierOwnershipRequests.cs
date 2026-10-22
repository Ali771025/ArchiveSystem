using System.Text.Json;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.Dossiers.Models;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Application.Features.Dossiers.Ownership;

public sealed record GetDossierOwnersQuery(long? DossierId, string? CaseNumber)
    : IRequest<IReadOnlyCollection<DossierOwnerItemResponse>>;

public sealed record AttachDossierOwnerCommand(long? DossierId, string? CaseNumber, long? OwnerId, Guid? TaxpayerId)
    : IRequest;

public sealed record DetachDossierOwnerCommand(long? DossierId, string? CaseNumber, long OwnerId)
    : IRequest;

public sealed record UploadDossierAvatarCommand(long DossierId, IFormFile File)
    : IRequest<DossierAvatarUploadResponse>;

public sealed class DossierOwnershipHandler :
    IRequestHandler<GetDossierOwnersQuery, IReadOnlyCollection<DossierOwnerItemResponse>>,
    IRequestHandler<AttachDossierOwnerCommand>,
    IRequestHandler<DetachDossierOwnerCommand>,
    IRequestHandler<UploadDossierAvatarCommand, DossierAvatarUploadResponse>
{
    private readonly IArchiveDbContext _dbContext;
    private readonly IAccessControlService _accessControlService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaseAvatarService _caseAvatarService;

    public DossierOwnershipHandler(
        IArchiveDbContext dbContext,
        IAccessControlService accessControlService,
        ICurrentUserContext currentUser,
        ICaseAvatarService caseAvatarService)
    {
        _dbContext = dbContext;
        _accessControlService = accessControlService;
        _currentUser = currentUser;
        _caseAvatarService = caseAvatarService;
    }

    public async Task<IReadOnlyCollection<DossierOwnerItemResponse>> Handle(
        GetDossierOwnersQuery request,
        CancellationToken cancellationToken)
    {
        var caseEntity = await ResolveCaseAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (caseEntity != null)
        {
            await EnsureCanReadAsync(caseEntity.ClassificationId, cancellationToken);
            return ParseCaseOwners(caseEntity.Owners)
                .OrderBy(owner => owner.NameOrTitle)
                .ThenBy(owner => owner.OwnerId)
                .ToList();
        }

        var dossier = await ResolveDossierAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (dossier != null)
        {
            await EnsureCanReadAsync(dossier.ClassificationId, cancellationToken);
            return await _dbContext.DossierOwners
                .AsNoTracking()
                .Where(link => link.DossierId == dossier.Id && link.DeletedAt == null)
                .OrderBy(link => link.Owner.CompanyName)
                .ThenBy(link => link.Owner.FirstName)
                .ThenBy(link => link.Owner.LastName)
                .Select(link => new DossierOwnerItemResponse
                {
                    OwnerId = link.OwnerId,
                    OwnerType = link.Owner.OwnerType,
                    NameOrTitle = link.Owner.OwnerType == OwnerType.Natural
                        ? ((link.Owner.FirstName ?? string.Empty) + " " + (link.Owner.LastName ?? string.Empty)).Trim()
                        : (link.Owner.CompanyName ?? string.Empty),
                    NationalIdentifier = link.Owner.OwnerType == OwnerType.Natural
                        ? (link.Owner.NationalCode ?? string.Empty)
                        : (link.Owner.NationalId ?? string.Empty),
                    TaxpayerId = link.Owner.TaxpayerId,
                    Mobile = link.Owner.Mobile,
                    HomePhone = link.Owner.HomePhone
                })
                .ToListAsync(cancellationToken);
        }

        throw new NotFoundException("Case/Dossier was not found.");
    }

    public async Task Handle(AttachDossierOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await ResolveOwnerForAttachmentAsync(request.OwnerId, request.TaxpayerId, cancellationToken);

        var caseEntity = await ResolveCaseAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (caseEntity != null)
        {
            await EnsureCanWriteAsync(caseEntity.ClassificationId, cancellationToken);
            var snapshots = ParseCaseOwnerSnapshots(caseEntity.Owners);
            if (snapshots.Any(x => x.Id == owner.Id))
            {
                throw new ConflictException($"Owner '{owner.Id}' is already attached.");
            }

            snapshots.Add(new CaseOwnerSnapshot
            {
                Id = owner.Id,
                FullName = owner.GetDisplayName(),
                NationalCode = owner.GetIdentifier(),
                Phone = owner.Mobile ?? owner.HomePhone ?? string.Empty
            });
            caseEntity.UpdateOwners(JsonSerializer.Serialize(snapshots));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var dossier = await ResolveDossierAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (dossier != null)
        {
            await EnsureCanWriteAsync(dossier.ClassificationId, cancellationToken);
            var existing = await _dbContext.DossierOwners
                .FirstOrDefaultAsync(link => link.DossierId == dossier.Id && link.OwnerId == owner.Id, cancellationToken);

            if (existing != null)
            {
                if (!existing.IsDeleted)
                {
                    throw new ConflictException($"Owner '{owner.Id}' is already attached to dossier '{dossier.Id}'.");
                }

                existing.Restore();
            }
            else
            {
                _dbContext.DossierOwners.Add(new DossierOwner(0, dossier.Id, owner.Id));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new NotFoundException("Case/Dossier was not found.");
    }

    private async Task<Owner> ResolveOwnerForAttachmentAsync(long? ownerId, Guid? taxpayerId, CancellationToken cancellationToken)
    {
        if (ownerId.HasValue && ownerId.Value > 0)
        {
            return await _dbContext.Owners
                .AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == ownerId.Value, cancellationToken)
                ?? throw new NotFoundException($"Owner '{ownerId.Value}' was not found.");
        }

        if (!taxpayerId.HasValue || taxpayerId.Value == Guid.Empty)
        {
            throw new ValidationException("OwnerId or TaxpayerId is required.");
        }

        var taxpayer = await _dbContext.Taxpayers
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == taxpayerId.Value && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException($"Taxpayer '{taxpayerId.Value}' was not found.");

        var isNatural = taxpayer.TaxpayerType == TaxpayerType.NaturalPerson;
        var identifier = isNatural ? taxpayer.NationalId : taxpayer.NationalCompanyId;
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ValidationException("Taxpayer national identifier is required.");
        }

        var existingOwner = isNatural
            ? await _dbContext.Owners.AsNoTracking().FirstOrDefaultAsync(owner => owner.NationalCode == identifier, cancellationToken)
            : await _dbContext.Owners.AsNoTracking().FirstOrDefaultAsync(owner => owner.NationalId == identifier, cancellationToken);

        if (existingOwner is not null)
        {
            return existingOwner;
        }

        var ownerType = isNatural ? OwnerType.Natural : OwnerType.Legal;
        var owner = new Owner(
            id: 0,
            ownerType: ownerType,
            mobile: taxpayer.Mobile,
            postalCode: taxpayer.PostalCode,
            address: taxpayer.Address,
            homePhone: taxpayer.LandlinePhone,
            firstName: isNatural ? taxpayer.FirstName : null,
            lastName: isNatural ? taxpayer.LastName : null,
            nationalCode: isNatural ? taxpayer.NationalId : null,
            identityNumber: isNatural ? taxpayer.IdentityNumber : null,
            companyName: isNatural ? null : taxpayer.CompanyName,
            nationalId: isNatural ? null : taxpayer.NationalCompanyId);

        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return owner;
    }

    public async Task Handle(DetachDossierOwnerCommand request, CancellationToken cancellationToken)
    {
        var caseEntity = await ResolveCaseAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (caseEntity != null)
        {
            await EnsureCanWriteAsync(caseEntity.ClassificationId, cancellationToken);
            var snapshots = ParseCaseOwnerSnapshots(caseEntity.Owners);
            if (snapshots.RemoveAll(x => x.Id == request.OwnerId) == 0)
            {
                throw new NotFoundException("Owner was not attached.");
            }

            caseEntity.UpdateOwners(JsonSerializer.Serialize(snapshots));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var dossier = await ResolveDossierAsync(request.DossierId, request.CaseNumber, cancellationToken);
        if (dossier != null)
        {
            await EnsureCanWriteAsync(dossier.ClassificationId, cancellationToken);
            var link = await _dbContext.DossierOwners
                .FirstOrDefaultAsync(x => x.DossierId == dossier.Id && x.OwnerId == request.OwnerId && x.DeletedAt == null, cancellationToken)
                ?? throw new NotFoundException("Dossier owner link was not found.");

            link.SoftDelete();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new NotFoundException("Case/Dossier was not found.");
    }

    public async Task<DossierAvatarUploadResponse> Handle(
        UploadDossierAvatarCommand request,
        CancellationToken cancellationToken)
    {
        ValidateAvatar(request.File);

        var caseEntity = await ResolveCaseAsync(request.DossierId, null, cancellationToken);
        if (caseEntity != null)
        {
            await EnsureCanWriteAsync(caseEntity.ClassificationId, cancellationToken);
            var avatarPath = await _caseAvatarService.UploadAvatarAsync(caseEntity.Id, request.File, cancellationToken);
            caseEntity.UpdateAvatarImagePath(avatarPath);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return BuildAvatarResponse(avatarPath);
        }

        var dossier = await ResolveDossierAsync(request.DossierId, null, cancellationToken)
            ?? throw new NotFoundException("Dossier was not found.");

        await EnsureCanWriteAsync(dossier.ClassificationId, cancellationToken);
        var dossierAvatarPath = await _caseAvatarService.UploadAvatarAsync(dossier.Id, request.File, cancellationToken);
        dossier.UpdateStaticFields(
            dossier.DossierNumber,
            dossier.ExternalCaseNumber,
            dossier.Address,
            dossier.UsageType,
            dossier.Latitude,
            dossier.Longitude,
            dossierAvatarPath,
            dossier.RenovationCode,
            dossier.PostalCode,
            dossier.RegistrationPlate);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BuildAvatarResponse(dossierAvatarPath);
    }

    private async Task<Dossier?> ResolveDossierAsync(long? dossierId, string? caseNumber, CancellationToken cancellationToken)
    {
        if (!dossierId.HasValue)
        {
            return null;
        }

        var id = dossierId.Value;
        var key = id.ToString();
        return await _dbContext.Dossiers
            .FirstOrDefaultAsync(d => d.DeletedAt == null &&
                (d.Id == id || d.DossierNumber == key || d.ExternalCaseNumber == key), cancellationToken);
    }

    private async Task<Case?> ResolveCaseAsync(long? dossierId, string? caseNumber, CancellationToken cancellationToken)
    {
        if (dossierId.HasValue)
        {
            var id = dossierId.Value;
            var key = id.ToString();
            return await _dbContext.Cases
                .FirstOrDefaultAsync(c => c.DeletedAt == null && (c.Id == id || c.CaseNumber == key), cancellationToken);
        }

        var normalized = NormalizeKey(caseNumber);
        if (normalized == null)
        {
            return null;
        }

        return await _dbContext.Cases
            .FirstOrDefaultAsync(c => c.DeletedAt == null && c.CaseNumber == normalized, cancellationToken);
    }

    private async Task EnsureCanReadAsync(long classificationId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("Superuser"))
        {
            return;
        }

        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("Authenticated user context is required.");
        if (!await _accessControlService.CanReadClassificationAsync(userId, classificationId, cancellationToken))
        {
            throw new ForbiddenAccessException("User is not allowed to read this dossier.");
        }
    }

    private async Task EnsureCanWriteAsync(long classificationId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("Superuser"))
        {
            return;
        }

        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("Authenticated user context is required.");
        if (!await _accessControlService.CanWriteClassificationAsync(userId, classificationId, cancellationToken))
        {
            throw new ForbiddenAccessException("User is not allowed to update this dossier.");
        }
    }

    private static IReadOnlyCollection<DossierOwnerItemResponse> ParseCaseOwners(string? ownersJson)
    {
        return ParseCaseOwnerSnapshots(ownersJson)
            .Select(snapshot => new DossierOwnerItemResponse
            {
                OwnerId = snapshot.Id,
                OwnerType = OwnerType.Natural,
                NameOrTitle = snapshot.FullName,
                NationalIdentifier = snapshot.NationalCode,
                Mobile = snapshot.Phone
            })
            .ToList();
    }

    private static List<CaseOwnerSnapshot> ParseCaseOwnerSnapshots(string? ownersJson)
    {
        if (string.IsNullOrWhiteSpace(ownersJson))
        {
            return new List<CaseOwnerSnapshot>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CaseOwnerSnapshot>>(ownersJson) ?? new List<CaseOwnerSnapshot>();
        }
        catch (JsonException)
        {
            return new List<CaseOwnerSnapshot>();
        }
    }

    private static void ValidateAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("A non-empty image file is required.");
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            throw new ValidationException("The avatar image size must be less than or equal to 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
        {
            throw new ValidationException("Only .jpg, .jpeg, and .png files are allowed.");
        }
    }

    private static DossierAvatarUploadResponse BuildAvatarResponse(string avatarPath)
    {
        return new DossierAvatarUploadResponse
        {
            AvatarImagePath = avatarPath,
            ThumbnailImagePath = BuildThumbnailPath(avatarPath)
        };
    }

    private static string BuildThumbnailPath(string avatarPath)
    {
        var extension = Path.GetExtension(avatarPath);
        return string.IsNullOrWhiteSpace(extension)
            ? avatarPath
            : avatarPath[..^extension.Length] + "_thumb" + extension;
    }

    private static string? NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class CaseOwnerSnapshot
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
