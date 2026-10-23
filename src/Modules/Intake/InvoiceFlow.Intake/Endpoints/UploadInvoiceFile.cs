using InvoiceFlow.Intake.Files;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Intake.Endpoints;

internal static class UploadInvoiceFile
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static async Task<IResult> HandleAsync(IFormFile file, IInvoiceFileStore store, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "File is empty"
            });
        }
        if (file.Length > MaxFileSizeBytes)
        {
            return Results.BadRequest(new
            {
                error = "File is larger than 10 MB"
            });
        }
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                error = "File must be a PDF"
            });
        }

        await using Stream content = file.OpenReadStream();

        StoredFile storedFile = await store.SaveAsync(content, file.FileName, cancellationToken);

        return Results.Created(
            $"/api/intake/files/{storedFile.FileId}",
            storedFile);
    }
}
