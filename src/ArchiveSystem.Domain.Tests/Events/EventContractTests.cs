using ArchiveSystem.Domain.Events;
using Xunit;

namespace ArchiveSystem.Domain.Tests.Events
{
    public class EventContractTests
    {
        [Fact]
        public void OcrCompletedEvent_DoesNotExposePathFields()
        {
            var propertyNames = typeof(OcrCompletedEvent).GetProperties().Select(x => x.Name).ToArray();

            Assert.DoesNotContain(propertyNames, x => x.Contains("Path", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nameof(OcrCompletedEvent.FileVersionId), propertyNames);
            Assert.Contains(nameof(OcrCompletedEvent.WordCount), propertyNames);
            Assert.Contains(nameof(OcrCompletedEvent.HasText), propertyNames);
        }

        [Fact]
        public void ThumbnailGeneratedEvent_DoesNotExposePathFields()
        {
            var propertyNames = typeof(ThumbnailGeneratedEvent).GetProperties().Select(x => x.Name).ToArray();

            Assert.DoesNotContain(propertyNames, x => x.Contains("Path", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nameof(ThumbnailGeneratedEvent.FileVersionId), propertyNames);
            Assert.Contains(nameof(ThumbnailGeneratedEvent.BytesLength), propertyNames);
            Assert.Contains(nameof(ThumbnailGeneratedEvent.Width), propertyNames);
            Assert.Contains(nameof(ThumbnailGeneratedEvent.Height), propertyNames);
        }
    }
}
