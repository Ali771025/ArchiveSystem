// src/ArchiveSystem.Domain/Specifications/FileByCaseIdSpecification.cs
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Domain.Specifications
{
    public class FileByCaseIdSpecification : Specification<FileEntity>
    {
        public FileByCaseIdSpecification(long caseId)
        {
            AddCriteria(f => f.CaseId == caseId);
            AddInclude(f => f.Case!);
        }
    }
}
