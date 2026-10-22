using ArchiveSystem.Worker.Ocr.Services;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class PaddleArabicTextDecoderTests
    {
        [Theory]
        [InlineData("فسوي", "یوسف")]
        [InlineData("ىنارمع", "عمرانی")]
        [InlineData("١٣٧٧/١٠/٢٥ دلوت خيرات", "تاریخ تولد 1377/10/25")]
        [InlineData("ABC-123", "ABC-123")]
        public void RestoreLogicalOrder_RestoresArabicTextAndPreservesNumericRuns(
            string prediction,
            string expected)
        {
            var actual = PaddleArabicTextDecoder.RestoreLogicalOrder(prediction);

            Assert.Equal(expected, PersianOcrText.Normalize(actual, false));
        }
    }
}
