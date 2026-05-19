namespace NeuroScan.Application.IServices;

public interface IMriImageService
{
    Task<(string? DirPath, int Count)> SaveSegmentationSlicesAsync(Guid scanId, IList<string> base64Slices);
    Task<(string? DirPath, int Count)> SaveTumorOverlaySlicesAsync(Guid scanId, IList<string> base64Slices);
    Task<int> SaveRawSlicesAsync(Guid scanId, IList<string> base64Slices);
    Task<byte[]?> GetRawSliceAsync(Guid scanId, int sliceIndex);
    Task<bool> HasRawSliceCacheAsync(Guid scanId, int? expectedCount);
    Task<int> GetCachedRawSliceCountAsync(Guid scanId);
    Task InvalidateRawSliceCacheAsync(Guid scanId);
    Task SaveCorrectedSliceAsync(Guid scanId, int sliceIndex, string base64Png);
    Task<byte[]?> GetCorrectedSliceAsync(Guid scanId, int sliceIndex);
}
