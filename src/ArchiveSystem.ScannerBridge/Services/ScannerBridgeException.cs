namespace ArchiveSystem.ScannerBridge.Services;

public sealed class ScannerBridgeException : Exception
{
    public ScannerBridgeException(string code, string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}
