namespace ArchiveSystem.Worker.Ocr.Options
{
    public sealed class RapidOcrEngineOptions
    {
        public const string SectionName = "RapidOcr";

        public string DetectionModelPath { get; set; } = "models/v5/ch_PP-OCRv5_mobile_det.onnx";
        public string ClassificationModelPath { get; set; } = "models/v5/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx";
        public string RecognitionModelPath { get; set; } = "models/persian-v5/rec.onnx";
        public string DictionaryPath { get; set; } = "models/persian-v5/dict.txt";
        public int ThreadCount { get; set; } = 4;
        public int ImgResize { get; set; } = 1400;
        public float TextScore { get; set; } = 0.50f;
        public bool DoAngle { get; set; } = true;
        public int PdfDpi { get; set; } = 200;
        public bool EnablePdfNativeTextExtraction { get; set; } = true;
        public int PdfNativeTextMinimumCharacters { get; set; } = 40;
        public bool OcrPdfImagesWhenNativeTextExists { get; set; } = true;
        public float PdfImageOcrMinimumCoverage { get; set; } = 0.08f;
        public long MaxInputBytes { get; set; } = 209_715_200;
        public string? TempDirectory { get; set; }
        public bool EnableIdentityDocumentMode { get; set; } = true;
        public float MinimumAcceptedConfidence { get; set; } = 0.35f;
    }
}
