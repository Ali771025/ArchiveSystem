using ArchiveSystem.Domain.Entities;
using Xunit;

namespace ArchiveSystem.Domain.Tests.Entities
{
    public class CaseAndClassificationTests
    {
        [Fact]
        public void MoveToClassification_UpdatesClassificationId()
        {
            var caseEntity = new Case(1, "CASE-1", "Address", 10);

            caseEntity.MoveToClassification(20);

            Assert.Equal(20, caseEntity.ClassificationId);
        }

        [Fact]
        public void MissingExternalCaseNumber_RemainsEmptyAndDoesNotCopyCaseNumber()
        {
            var caseEntity = new Case(1, "CASE-1", "Address", 10);

            Assert.Equal("CASE-1", caseEntity.CaseNumber);
            Assert.Null(caseEntity.ExternalCaseNumber);
        }

        [Fact]
        public void UpdatingExternalCaseNumber_DoesNotChangeCaseNumber()
        {
            var caseEntity = new Case(
                1,
                "CASE-1",
                "Address",
                10,
                externalCaseNumber: "REF-1");

            caseEntity.UpdateExternalCaseNumber("REF-2");

            Assert.Equal("CASE-1", caseEntity.CaseNumber);
            Assert.Equal("REF-2", caseEntity.ExternalCaseNumber);
        }

        [Fact]
        public void ExternalCaseNumber_CanBeClearedWithoutChangingCaseNumber()
        {
            var caseEntity = new Case(
                1,
                "CASE-1",
                "Address",
                10,
                externalCaseNumber: "REF-1");

            caseEntity.UpdateExternalCaseNumber(null);

            Assert.Equal("CASE-1", caseEntity.CaseNumber);
            Assert.Null(caseEntity.ExternalCaseNumber);
        }

        [Fact]
        public void RebaseHierarchy_UpdatesPathAndLevel()
        {
            var classification = new Classification(5, "Node");

            classification.RebaseHierarchy("/1/3/5/", 2);

            Assert.Equal("/1/3/5/", classification.Path);
            Assert.Equal(2, classification.Level);
        }
    }
}
