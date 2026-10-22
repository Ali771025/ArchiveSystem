using ArchiveSystem.ScannerBridge.Models;

namespace ArchiveSystem.ScannerBridge.Services;

public interface IScannerService
{
    Task<IReadOnlyList<ScannerDeviceResponse>> GetDevicesAsync(CancellationToken cancellationToken);
    Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken);
}
