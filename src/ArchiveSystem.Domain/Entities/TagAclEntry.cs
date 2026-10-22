using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Domain.Entities
{
    public class TagAclEntry : BaseEntity<long>, IAggregateRoot
    {
        public AccessSubjectType SubjectType { get; private set; }
        public long SubjectId { get; private set; }
        public long TagId { get; private set; }
        public TagPermissionMask PermissionMask { get; private set; }

        public Tag Tag { get; private set; } = null!;

        private TagAclEntry() : base()
        {
        }

        public TagAclEntry(
            long id,
            AccessSubjectType subjectType,
            long subjectId,
            long tagId,
            TagPermissionMask permissionMask) : base(id)
        {
            SubjectType = subjectType;
            SubjectId = subjectId;
            TagId = tagId;
            PermissionMask = permissionMask;
        }

        public void UpdateMask(TagPermissionMask permissionMask)
        {
            PermissionMask = permissionMask;
            MarkAsModified();
            AddDomainEvent(new TagAclChangedEvent(TagId));
        }
    }
}
