// src/ArchiveSystem.Infrastructure/Persistence/ArchiveDbContext.cs (Updated)
using Audit.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Infrastructure.Identity;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Persistence
{
    public class ArchiveDbContext : AuditIdentityDbContext<ApplicationUser, IdentityRole<long>, long>, IArchiveDbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : base(options)
        {
        }

        public ArchiveDbContext(
            DbContextOptions<ArchiveDbContext> options,
            IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<FileEntity> Files { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Classification> Classifications { get; set; }
        public DbSet<Role> AccessRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> AccessUserRoles { get; set; }
        public DbSet<ClassificationAclEntry> ClassificationAclEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagAclEntry> TagAclEntries { get; set; }
        public DbSet<FileTag> FileTags { get; set; }
        public DbSet<UserCaseAccess> UserCaseAccesses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }
        public DbSet<UploadSession> UploadSessions { get; set; }
        public DbSet<UploadPart> UploadParts { get; set; }
        public DbSet<DynamicTemplate> DynamicTemplates { get; set; }
        public DbSet<DynamicFieldGroup> DynamicFieldGroups { get; set; }
        public DbSet<DynamicFieldDefinition> DynamicFieldDefinitions { get; set; }
        public DbSet<DynamicFieldOption> DynamicFieldOptions { get; set; }
        public DbSet<CaseFieldValueString> CaseFieldValueStrings { get; set; }
        public DbSet<CaseFieldValueNumber> CaseFieldValueNumbers { get; set; }
        public DbSet<CaseFieldValueDate> CaseFieldValueDates { get; set; }
        public DbSet<CaseFieldValueBool> CaseFieldValueBools { get; set; }
        public DbSet<CaseFieldValueOption> CaseFieldValueOptions { get; set; }
        public DbSet<CaseLoan> CaseLoans { get; set; }
        public DbSet<Dossier> Dossiers { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<DossierOwner> DossierOwners { get; set; }
        public DbSet<DossierDynamicFieldGroup> DossierDynamicFieldGroups { get; set; }
        public DbSet<DossierDynamicField> DossierDynamicFields { get; set; }
        public DbSet<DossierDynamicFieldOption> DossierDynamicFieldOptions { get; set; }
        public DbSet<DossierDynamicFieldValue> DossierDynamicFieldValues { get; set; }
        public DbSet<Taxpayer> Taxpayers { get; set; }
        public DbSet<MovementLog> MovementLogs { get; set; }
        public DbSet<PhysicalMovementLog> PhysicalMovementLogs { get; set; }
        public DbSet<ExportJob> ExportJobs { get; set; }
        public DbSet<AuditTrailLog> AuditTrailLogs { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<SmsOutboxMessage> SmsOutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArchiveDbContext).Assembly);
            
            base.OnModelCreating(modelBuilder);

            // Configure table partitioning for Files (will be handled via migrations)
            modelBuilder.Entity<FileEntity>().ToTable("Files");
        }

        public override int SaveChanges()
        {
            AddAuditCustomField("UserId", ResolveUserId());
            AddAuditCustomField("ClientIpAddress", ResolveClientIpAddress());
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditCustomField("UserId", ResolveUserId());
            AddAuditCustomField("ClientIpAddress", ResolveClientIpAddress());
            return SaveChangesAsync(true, cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var domainEvents = ChangeTracker.Entries<BaseEntity<long>>()
                .Where(e => e.Entity.DomainEvents.Any())
                .SelectMany(e => e.Entity.DomainEvents)
                .Concat(ChangeTracker.Entries<BaseEntity<Guid>>()
                .Where(e => e.Entity.DomainEvents.Any())
                .SelectMany(e => e.Entity.DomainEvents)
                )
                .ToList();

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            // Clear domain events after saving
            foreach (var entry in ChangeTracker.Entries<BaseEntity<long>>())
            {
                entry.Entity.ClearDomainEvents();
            }

            foreach (var entry in ChangeTracker.Entries<BaseEntity<Guid>>())
            {
                entry.Entity.ClearDomainEvents();
            }

            return result;
        }

        private string? ResolveUserId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            var raw = user.FindFirst("sub")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }

        private string? ResolveClientIpAddress()
        {
            var ip = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress;
            return ip?.ToString();
        }
    }
}
