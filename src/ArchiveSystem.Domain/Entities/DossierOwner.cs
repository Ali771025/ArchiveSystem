using ArchiveSystem.Domain.Interfaces;

namespace ArchiveSystem.Domain.Entities
{
    public class DossierOwner : BaseEntity<long>, ISoftDeletable
    {
        public long DossierId { get; private set; }
        public long OwnerId { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public Dossier Dossier { get; private set; } = null!;
        public Owner Owner { get; private set; } = null!;

        private DossierOwner() : base()
        {
        }

        public DossierOwner(long id, long dossierId, long ownerId) : base(id)
        {
            if (dossierId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dossierId), "DossierId must be greater than zero.");
            }

            if (ownerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerId), "OwnerId must be greater than zero.");
            }

            DossierId = dossierId;
            OwnerId = ownerId;
        }

        public void SoftDelete()
        {
            if (IsDeleted)
            {
                return;
            }

            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            DeletedAt = null;
            MarkAsModified();
        }
    }
}
