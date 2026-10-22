using ArchiveSystem.Domain.Common;
using Xunit;

namespace ArchiveSystem.Domain.Tests.Common
{
    public sealed class PersianTextNormalizerTests
    {
        [Theory]
        [InlineData("  شهرسازي  هوشمند ", "شهرسازی هوشمند")]
        [InlineData("شهرسازی\u200Cهوشمند", "شهرسازی هوشمند")]
        [InlineData("كاربرد يکپارچه", "کاربرد یکپارچه")]
        public void NormalizeForSearch_NormalizesPersianLettersAndSpacing(
            string input,
            string expected)
        {
            Assert.Equal(expected, PersianTextNormalizer.NormalizeForSearch(input));
        }
    }
}
