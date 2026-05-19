using Moq;
using Microsoft.Extensions.Configuration;
using NeuroScan.Application.Services;

namespace NeuroScan.Tests.Services;

public class MriImageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MriImageService _sut;

    public MriImageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"neuroscan-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Storage:UploadPath"]).Returns(_tempDir);

        _sut = new MriImageService(configMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── HasRawSliceCacheAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task HasRawSliceCacheAsync_WhenDirectoryDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.HasRawSliceCacheAsync(Guid.NewGuid(), null);

        Assert.False(result);
    }

    [Fact]
    public async Task HasRawSliceCacheAsync_WhenDirectoryExistsWithNoExpectedCount_ReturnsTrue()
    {
        var scanId = Guid.NewGuid();
        Directory.CreateDirectory(Path.Combine(_tempDir, "raw-slices", scanId.ToString()));

        var result = await _sut.HasRawSliceCacheAsync(scanId, null);

        Assert.True(result);
    }

    [Fact]
    public async Task HasRawSliceCacheAsync_WhenCountMatchesExpected_ReturnsTrue()
    {
        var scanId = Guid.NewGuid();
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);
        await File.WriteAllBytesAsync(Path.Combine(rawDir, "0.png"), new byte[] { 1, 2, 3 });
        await File.WriteAllBytesAsync(Path.Combine(rawDir, "1.png"), new byte[] { 4, 5, 6 });

        var result = await _sut.HasRawSliceCacheAsync(scanId, 2);

        Assert.True(result);
    }

    [Fact]
    public async Task HasRawSliceCacheAsync_WhenCountDoesNotMatch_ReturnsFalse()
    {
        var scanId = Guid.NewGuid();
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);
        await File.WriteAllBytesAsync(Path.Combine(rawDir, "0.png"), new byte[] { 1, 2 });

        var result = await _sut.HasRawSliceCacheAsync(scanId, 5);

        Assert.False(result);
    }

    // ── InvalidateRawSliceCacheAsync ───────────────────────────────────────────

    [Fact]
    public async Task InvalidateRawSliceCacheAsync_WhenDirectoryExists_DeletesIt()
    {
        var scanId = Guid.NewGuid();
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);

        await _sut.InvalidateRawSliceCacheAsync(scanId);

        Assert.False(Directory.Exists(rawDir));
    }

    [Fact]
    public async Task InvalidateRawSliceCacheAsync_WhenDirectoryDoesNotExist_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.InvalidateRawSliceCacheAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    // ── GetCachedRawSliceCountAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetCachedRawSliceCountAsync_WhenDirectoryDoesNotExist_ReturnsZero()
    {
        var result = await _sut.GetCachedRawSliceCountAsync(Guid.NewGuid());

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetCachedRawSliceCountAsync_WhenFilesExist_ReturnsCount()
    {
        var scanId = Guid.NewGuid();
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);
        for (int i = 0; i < 3; i++)
            await File.WriteAllBytesAsync(Path.Combine(rawDir, $"{i}.png"), new byte[] { 1 });

        var result = await _sut.GetCachedRawSliceCountAsync(scanId);

        Assert.Equal(3, result);
    }

    // ── SaveSegmentationSlicesAsync ────────────────────────────────────────────

    [Fact]
    public async Task SaveSegmentationSlicesAsync_CreatesFilesAndReturnsDirAndCount()
    {
        var scanId = Guid.NewGuid();
        // A tiny valid-ish base64 PNG (1x1 white pixel)
        var slices = new List<string>
        {
            Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            Convert.ToBase64String(new byte[] { 7, 8, 9 })
        };

        var (dirPath, count) = await _sut.SaveSegmentationSlicesAsync(scanId, slices);

        Assert.NotNull(dirPath);
        Assert.Equal(3, count);
        Assert.True(Directory.Exists(dirPath));
        Assert.Equal(3, Directory.GetFiles(dirPath, "*.png").Length);
    }

    [Fact]
    public async Task SaveSegmentationSlicesAsync_WhenCalledTwice_OverwritesPreviousSlices()
    {
        var scanId = Guid.NewGuid();
        var slices3 = Enumerable.Range(0, 3).Select(_ => Convert.ToBase64String(new byte[] { 1 })).ToList();
        var slices5 = Enumerable.Range(0, 5).Select(_ => Convert.ToBase64String(new byte[] { 2 })).ToList();

        await _sut.SaveSegmentationSlicesAsync(scanId, slices3);
        var (_, count) = await _sut.SaveSegmentationSlicesAsync(scanId, slices5);

        Assert.Equal(5, count);
    }

    // ── SaveRawSlicesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveRawSlicesAsync_CreatesFilesAndReturnsCount()
    {
        var scanId = Guid.NewGuid();
        var slices = Enumerable.Range(0, 4).Select(_ => Convert.ToBase64String(new byte[] { 1, 2, 3 })).ToList();

        var count = await _sut.SaveRawSlicesAsync(scanId, slices);

        Assert.Equal(4, count);
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Assert.Equal(4, Directory.GetFiles(rawDir, "*.png").Length);
    }

    // ── GetRawSliceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRawSliceAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetRawSliceAsync(Guid.NewGuid(), 0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRawSliceAsync_WhenFileExists_ReturnsBytes()
    {
        var scanId = Guid.NewGuid();
        var rawDir = Path.Combine(_tempDir, "raw-slices", scanId.ToString());
        Directory.CreateDirectory(rawDir);
        var expected = new byte[] { 10, 20, 30 };
        await File.WriteAllBytesAsync(Path.Combine(rawDir, "2.png"), expected);

        var result = await _sut.GetRawSliceAsync(scanId, 2);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    // ── SaveCorrectedSliceAsync / GetCorrectedSliceAsync ───────────────────────

    [Fact]
    public async Task GetCorrectedSliceAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetCorrectedSliceAsync(Guid.NewGuid(), 0);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndGetCorrectedSliceAsync_RoundTrip_ReturnsOriginalBytes()
    {
        var scanId = Guid.NewGuid();
        var bytes = new byte[] { 99, 88, 77, 66 };
        var base64 = Convert.ToBase64String(bytes);

        await _sut.SaveCorrectedSliceAsync(scanId, 3, base64);
        var result = await _sut.GetCorrectedSliceAsync(scanId, 3);

        Assert.NotNull(result);
        Assert.Equal(bytes, result);
    }
}
