using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Domain.Entities
{
    public class ClassificationAclEntry : BaseEntity<long>, IAggregateRoot
    {
        public AccessSubjectType SubjectType { get; private set; }
        public long SubjectId { get; private set; }
        public long ClassificationId { get; private set; }
        public ClassificationPermissionMask PermissionMask { get; private set; }
        public bool IsExplicit { get; private set; }
        public ClassificationAclInheritMode InheritMode { get; private set; }

        public Classification Classification { get; private set; } = null!;

        private ClassificationAclEntry() : base()
        {
        }

        public ClassificationAclEntry(
            long id,
            AccessSubjectType subjectType,
            long subjectId,
            long classificationId,
            ClassificationPermissionMask permissionMask,
            bool isExplicit = true,
            ClassificationAclInheritMode inheritMode = ClassificationAclInheritMode.Override) : base(id)
        {
            SubjectType = subjectType;
            SubjectId = subjectId;
            ClassificationId = classificationId;
            PermissionMask = permissionMask;
            IsExplicit = isExplicit;
            InheritMode = inheritMode;
        }

        public void UpdateMask(ClassificationPermissionMask permissionMask, ClassificationAclInheritMode inheritMode)
        {
            PermissionMask = permissionMask;
            InheritMode = inheritMode;
            IsExplicit = true;
            MarkAsModified();
            AddDomainEvent(new ClassificationAclChangedEvent(ClassificationId));
        }
    }
}
