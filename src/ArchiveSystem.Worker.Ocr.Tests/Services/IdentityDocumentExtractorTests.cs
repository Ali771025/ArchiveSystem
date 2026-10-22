using ArchiveSystem.Worker.Ocr.Services;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class IdentityDocumentExtractorTests
    {
        [Fact]
        public void Enrich_ExtractsAndValidatesIranianNationalIdCardFields()
        {
            var result = CreateResult(
                "کارت هوشمند ملی\nشماره ملی\n۱۲۳۴۵۶۷۸۹۱\nنام\nیوسف\nنام خانوادگی\nآزمایشی\nتاریخ تولد\n۱۳۷۷/۱۰/۲۵\nنام پدر\nمحمدرضا\nپایان اعتبار\n۱۴۰۶/۰۱/۰۹",
                new[]
                {
                    Line("کارت هوشمند ملی", 500, 20, 400),
                    Line("شماره ملی", 1000, 100), Line("۱۲۳۴۵۶۷۸۹۱", 650, 100),
                    Line("نام", 1000, 180), Line("یوسف", 650, 180),
                    Line("نام خانوادگی", 1000, 260), Line("آزمایشی", 650, 260),
                    Line("تاریخ تولد", 1000, 340), Line("۱۳۷۷/۱۰/۲۵", 650, 340),
                    Line("نام پدر", 1000, 420), Line("محمدرضا", 650, 420),
                    Line("پایان اعتبار", 1000, 500), Line("۱۴۰۶/۰۱/۰۹", 650, 500),
                });

            var enriched = IdentityDocumentExtractor.Enrich(result);

            Assert.Equal("NationalIdCard", enriched.DocumentType);
            Assert.Equal("iran-national-id-card-rtl", enriched.LayoutProfile);
            Assert.Equal(6, enriched.Fields.Count);
            Assert.Equal("1234567891", Field(enriched, "nationalCode").NormalizedValue);
            Assert.True(Field(enriched, "nationalCode").IsValid);
            Assert.Equal("یوسف", Field(enriched, "firstName").NormalizedValue);
            Assert.Equal("آزمایشی", Field(enriched, "lastName").NormalizedValue);
            Assert.Equal("1377/10/25", Field(enriched, "birthDate").NormalizedValue);
            Assert.Equal("محمدرضا", Field(enriched, "fatherName").NormalizedValue);
            Assert.Equal("1406/01/09", Field(enriched, "expiryDate").NormalizedValue);
            Assert.DoesNotContain("identity-document-fields-require-review", enriched.Warnings);
        }

        [Fact]
        public void Enrich_ExtractsBirthCertificateFields()
        {
            var result = CreateResult(
                "شناسنامه جمهوری اسلامی ایران\nشماره شناسنامه: ۴۲۱\nشماره ملی: ۱۲۳۴۵۶۷۸۹۱\nنام: سارا\nنام خانوادگی: نمونه\nنام پدر: علی\nتاریخ تولد: ۱۳۸۰/۰۲/۱۲\nمحل تولد: تهران",
                Array.Empty<OcrLineResult>());

            var enriched = IdentityDocumentExtractor.Enrich(result);

            Assert.Equal("BirthCertificate", enriched.DocumentType);
            Assert.Equal("421", Field(enriched, "certificateNumber").NormalizedValue);
            Assert.Equal("تهران", Field(enriched, "birthPlace").NormalizedValue);
            Assert.True(Field(enriched, "nationalCode").IsValid);
        }

        [Fact]
        public void Enrich_ToleratesObservedOcrErrorsInIdentityLabels()
        {
            var result = CreateResult(
                "شماره ملی\n۱۲۳۴۵۶۷۸۹۱\nیوسف\nتم =\nعمرانی\nخانوادکی\nمحمدرضا\nتام پدر\nپایان اعتبار",
                new[]
                {
                    Line("شماره ملی", 1000, 100), Line("۱۲۳۴۵۶۷۸۹۱", 650, 100),
                    Line("تم =", 1000, 180), Line("یوسف", 650, 180),
                    Line("خانوادکی", 1000, 260), Line("عمرانی", 650, 260),
                    Line("تام پدر", 1000, 420), Line("محمدرضا", 650, 420),
                    Line("پایان اعتبار", 1000, 500)
                });

            var enriched = IdentityDocumentExtractor.Enrich(result);

            Assert.Equal("NationalIdCard", enriched.DocumentType);
            Assert.Equal("یوسف", Field(enriched, "firstName").NormalizedValue);
            Assert.Equal("عمرانی", Field(enriched, "lastName").NormalizedValue);
            Assert.Equal("محمدرضا", Field(enriched, "fatherName").NormalizedValue);
        }

        [Theory]
        [InlineData("۱۲۳۴۵۶۷۸۹۱", true)]
        [InlineData("1234567890", false)]
        [InlineData("1111111111", false)]
        [InlineData("123", false)]
        public void IsValidIranianNationalCode_UsesOfficialCheckDigitRule(string value, bool expected)
        {
            Assert.Equal(expected, IdentityDocumentExtractor.IsValidIranianNationalCode(value));
        }

        [Fact]
        public void PersianOcrText_NormalizesArabicLettersDigitsAndDirectionMarks()
        {
            var normalized = PersianOcrText.Normalize("\u200fعلي كريمي ١٢۳۴", preserveLines: false);

            Assert.Equal("علی کریمی 1234", normalized);
        }

        private static OcrResult CreateResult(string text, IReadOnlyList<OcrLineResult> lines) => new()
        {
            Engine = "test",
            Language = "fa",
            Confidence = 0.92f,
            PageCount = 1,
            WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            HasText = true,
            Text = text,
            Pages = new[]
            {
                new OcrPageResult
                {
                    PageNumber = 1,
                    Confidence = 0.92f,
                    Width = 1280,
                    Height = 807,
                    Text = text,
                    Lines = text.Split('\n'),
                    LineElements = lines
                }
            }
        };

        private static OcrLineResult Line(string text, int left, int top, int width = 180) => new()
        {
            Text = text,
            Confidence = 0.92f,
            Left = left,
            Top = top,
            Width = width,
            Height = 36
        };

        private static OcrFieldResult Field(OcrResult result, string key) =>
            Assert.Single(result.Fields, field => field.Key == key);
    }
}
