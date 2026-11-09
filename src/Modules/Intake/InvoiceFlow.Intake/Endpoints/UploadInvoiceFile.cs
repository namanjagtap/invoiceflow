using InvoiceFlow.Intake.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Intake.Endpoints;

internal static class UploadInvoiceFile
{
    private const string FileField = "file";
    private static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;

    private static ValidationProblem FileError(string message) => TypedResults.ValidationProblem(
        new Dictionary<string, string[]>
        {
            [FileField] = [message],
        });

    public static async Task<Results<Created<UploadFileResponse>, ValidationProblem>> HandleAsync(
        IFormFile file,
        IInvoiceFileStore store,
        IOptions<IntakeOptions> options,
        CancellationToken cancellationToken)
    {
        long maxBytes = options.Value.MaxUploadSizeBytes;

        if (file.Length == 0)
        {
            return FileError("File is empty.");
        }
        if (file.Length > maxBytes)
        {
            return FileError($"File is larger than {maxBytes / (1024 * 1024)} MB.");
        }
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return FileError("File must be a PDF.");
        }

        byte[] signature = new byte[5];
        int bytesRead = 0;

        await using (Stream signatureStream = file.OpenReadStream())
        {
            while (bytesRead < signature.Length)
            {
                int read = await signatureStream.ReadAsync(
                    signature.AsMemory(bytesRead),
                    cancellationToken);

                if (read == 0)
                {
                    break;
                }
                bytesRead += read;
            }
        }

        if (bytesRead != signature.Length || !signature.AsSpan().SequenceEqual(PdfSignature))
        {
            return FileError("The uploaded file does not have a valid PDF signature.");
        }

        await using Stream content = file.OpenReadStream();

        StoredFile storedFile = await store.SaveAsync(content, file.FileName, cancellationToken);

        UploadFileResponse response = new(
            storedFile.FileId,
            storedFile.FileName,
            storedFile.SizeBytes,
            storedFile.Sha256);

        return TypedResults.Created((string?)null, response);
    }
}
