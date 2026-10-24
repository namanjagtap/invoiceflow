using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;

namespace InvoiceFlow.Intake.Files;

internal sealed record StoredFile(
    Guid FileId,
    string FileName,
    long SizeBytes,
    string Sha256);

internal interface IInvoiceFileStore
{
    Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken);
}
internal sealed class LocalInvoiceFileStore(IOptions<IntakeOptions> options) : IInvoiceFileStore
{
    private readonly string _rootPath = options.Value.StoragePath;

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fileName);

        Guid fileId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;

        string folder = Path.Combine(
            _rootPath,
            utcNow.ToString("yyyy", CultureInfo.InvariantCulture),
            utcNow.ToString("MM", CultureInfo.InvariantCulture));

        Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, $"{fileId}.pdf");

        try
        {
            await using (var file = new FileStream(
                path,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Options = FileOptions.Asynchronous
                }))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            byte[] hash;

            await using (var stream = new FileStream(
                path,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Options = FileOptions.Asynchronous
                }))
            {
                hash = await SHA256.HashDataAsync(
                    stream,
                    cancellationToken);
            }

            long sizeBytes = new FileInfo(path).Length;

            return new StoredFile(
                fileId,
                Path.GetFileName(fileName),
                sizeBytes,
                Convert.ToHexStringLower(hash));
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }
}
