using Microsoft.Extensions.Configuration;
using NeuroScan.Application.IServices;

namespace NeuroScan.Application.Services;

public class MriImageService : IMriImageService
{
    private readonly string _uploadPath;

    public MriImageService(IConfiguration configuration)
    {
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads/scans";
    }

    public async Task<(string? DirPath, int Count)> SaveSegmentationSlicesAsync(Guid scanId, IList<string> base64Slices)
    {
        var imgDir = Path.Combine(_uploadPath, "segmentation-images");
        Directory.CreateDirectory(imgDir);
        var segDir = Path.Combine(imgDir, scanId.ToString());

        if (Directory.Exists(segDir))
            Directory.Delete(segDir, recursive: true);
        Directory.CreateDirectory(segDir);

        for (int i = 0; i < base64Slices.Count; i++)
        {
            var imgBytes = Convert.FromBase64String(base64Slices[i]);
            await File.WriteAllBytesAsync(Path.Combine(segDir, $"{i}.png"), imgBytes);
        }

        return (segDir, base64Slices.Count);
    }

    public async Task<(string? DirPath, int Count)> SaveTumorOverlaySlicesAsync(Guid scanId, IList<string> base64Slices)
    {
        var overlayRoot = Path.Combine(_uploadPath, "tumor-overlay-images");
        Directory.CreateDirectory(overlayRoot);
        var overlayDir = Path.Combine(overlayRoot, scanId.ToString());

        if (Directory.Exists(overlayDir))
            Directory.Delete(overlayDir, recursive: true);
        Directory.CreateDirectory(overlayDir);

        for (int i = 0; i < base64Slices.Count; i++)
        {
            var imgBytes = Convert.FromBase64String(base64Slices[i]);
            await File.WriteAllBytesAsync(Path.Combine(overlayDir, $"{i}.png"), imgBytes);
        }

        return (overlayDir, base64Slices.Count);
    }

    public async Task<int> SaveRawSlicesAsync(Guid scanId, IList<string> base64Slices)
    {
        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);

        for (int i = 0; i < base64Slices.Count; i++)
        {
            var bytes = Convert.FromBase64String(base64Slices[i]);
            await File.WriteAllBytesAsync(Path.Combine(rawDir, $"{i}.png"), bytes);
        }

        return base64Slices.Count;
    }

    public async Task<byte[]?> GetRawSliceAsync(Guid scanId, int sliceIndex)
    {
        var slicePath = Path.Combine(_uploadPath, "raw-slices", scanId.ToString(), $"{sliceIndex}.png");
        if (!File.Exists(slicePath)) return null;
        return await File.ReadAllBytesAsync(slicePath);
    }

    public Task<bool> HasRawSliceCacheAsync(Guid scanId, int? expectedCount)
    {
        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());
        if (!Directory.Exists(rawDir)) return Task.FromResult(false);

        var cachedCount = Directory.GetFiles(rawDir, "*.png").Length;
        if (expectedCount == null || expectedCount == 0) return Task.FromResult(true);

        return Task.FromResult(cachedCount == expectedCount);
    }

    public Task<int> GetCachedRawSliceCountAsync(Guid scanId)
    {
        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());
        if (!Directory.Exists(rawDir)) return Task.FromResult(0);
        return Task.FromResult(Directory.GetFiles(rawDir, "*.png").Length);
    }

    public Task InvalidateRawSliceCacheAsync(Guid scanId)
    {
        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());
        if (Directory.Exists(rawDir)) Directory.Delete(rawDir, recursive: true);
        return Task.CompletedTask;
    }

    public async Task SaveCorrectedSliceAsync(Guid scanId, int sliceIndex, string base64Png)
    {
        var corrDir = Path.Combine(_uploadPath, "corrected-slices", scanId.ToString());
        Directory.CreateDirectory(corrDir);

        var imgBytes = Convert.FromBase64String(base64Png);
        await File.WriteAllBytesAsync(Path.Combine(corrDir, $"{sliceIndex}.png"), imgBytes);
    }

    public async Task<byte[]?> GetCorrectedSliceAsync(Guid scanId, int sliceIndex)
    {
        var corrPath = Path.Combine(_uploadPath, "corrected-slices", scanId.ToString(), $"{sliceIndex}.png");
        if (!File.Exists(corrPath)) return null;
        return await File.ReadAllBytesAsync(corrPath);
    }
}
