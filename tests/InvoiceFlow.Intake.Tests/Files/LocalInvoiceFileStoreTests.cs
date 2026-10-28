using InvoiceFlow.Intake.Files;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace InvoiceFlow.Intake.Tests.Files;

public sealed class LocalInvoiceFileStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "invoiceflow-tests", Guid.NewGuid().ToString());
    private readonly LocalInvoiceFileStore _store;

    public LocalInvoiceFileStoreTests() =>
        _store = new LocalInvoiceFileStore(Options.Create(new IntakeOptions { StoragePath = _root }));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WritesFileAndReturnsHash()
    {
        byte[] bytes = "%PDF-1.4 test invoice"u8.ToArray();
        using var content = new MemoryStream(bytes);

        StoredFile stored = await _store.SaveAsync(content, "invoice.pdf", CancellationToken.None);

        string[] files = Directory.GetFiles(_root, "*.pdf", SearchOption.AllDirectories);
        Assert.Single(files);
        Assert.Equal(bytes.Length, stored.SizeBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), stored.Sha256);
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_ThrowsAndLeavesNoFile()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        byte[] bytes = "%PDF-1.4 test invoice"u8.ToArray();
        using var content = new MemoryStream(bytes);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _store.SaveAsync(content, "invoice.pdf", cts.Token));

        string[] files = Directory.Exists(_root)
            ? Directory.GetFiles(_root, "*.pdf", SearchOption.AllDirectories)
            : [];

        Assert.Empty(files);
    }

    [Fact]
    public async Task SaveAsync_TwoUploads_GetDifferentFileIds()
    {
        byte[] bytes = "%PDF-1.4 test invoice"u8.ToArray();

        using var firstContent = new MemoryStream(bytes);
        using var secondContent = new MemoryStream(bytes);

        StoredFile first = await _store.SaveAsync(
            firstContent,
            "invoice.pdf",
            CancellationToken.None);

        StoredFile second = await _store.SaveAsync(
            secondContent,
            "invoice.pdf",
            CancellationToken.None);

        Assert.NotEqual(first.FileId, second.FileId);
        Assert.Equal(first.Sha256, second.Sha256);   // same content gives the same hash, the basis of duplicate detection
    }

    [Fact]
    public async Task ExistsAsync_WhenFileWasSaved_ReturnsTrue()
    {
        using var content = new MemoryStream("%PDF-1.4 test invoice"u8.ToArray());

        StoredFile stored = await _store.SaveAsync(
           content,
           "invoice.pdf",
           CancellationToken.None);

        bool exists = await _store.ExistsAsync(
            stored.FileId,
            CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        bool exists = await _store.ExistsAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(exists);
    }
}
