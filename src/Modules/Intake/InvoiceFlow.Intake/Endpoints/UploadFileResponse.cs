namespace InvoiceFlow.Intake.Endpoints;

internal sealed record UploadFileResponse(
    Guid FileId,
    string FileName,
    long SizeBytes,
    string Sha256);