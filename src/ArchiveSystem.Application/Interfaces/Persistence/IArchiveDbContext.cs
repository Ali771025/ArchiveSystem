using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IArchiveDbContext
    {
        DbSet<ArchiveFile> Files { get; }
        DbSet<Case> Cases { get; }
        DbSet<Dossier> Dossiers { get; }
        DbSet<DossierOwner> DossierOwners { get; }
        DbSet<Owner> Owners { get; }
        DbSet<Taxpayer> Taxpayers { get; }
        DbSet<Classification> Classifications { get; }
        DbSet<FileTag> FileTags { get; }
        DbSet<Tag> Tags { get; }
        DbSet<DynamicFieldDefinition> DynamicFieldDefinitions { get; }
        DbSet<CaseFieldValueString> CaseFieldValueStrings { get; }
        DbSet<CaseFieldValueNumber> CaseFieldValueNumbers { get; }
        DbSet<CaseFieldValueDate> CaseFieldValueDates { get; }
        DbSet<CaseFieldValueBool> CaseFieldValueBools { get; }
        DbSet<CaseFieldValueOption> CaseFieldValueOptions { get; }
        DbSet<CaseLoan> CaseLoans { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
