using InvoiceFlow.Intake.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Intake.Endpoints;

internal static class UploadInvoiceFile
{
    private static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;

    public static async Task<IResult> HandleAsync(
        IFormFile file,
        IInvoiceFileStore store,
        IOptions<IntakeOptions> options,
        CancellationToken cancellationToken)
    {
        long maxBytes = options.Value.MaxUploadSizeBytes;

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "File is empty"
            });
        }
        if (file.Length > maxBytes)
        {
            return Results.BadRequest(new
            {
                error = $"File is larger than {maxBytes / (1024 * 1024)} MB"
            });
        }
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                error = "File must be a PDF"
            });
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
            return Results.BadRequest(new
            {
                error = "The uploaded file does not have a valid PDF signature."
            });
        }

        await using Stream content = file.OpenReadStream();

        StoredFile storedFile = await store.SaveAsync(content, file.FileName, cancellationToken);

        return Results.Created(
            $"/api/intake/files/{storedFile.FileId}",
            storedFile);
    }
}
