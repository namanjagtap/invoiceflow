using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Intake.Tests.Endpoints;

public sealed class UploadInvoiceFileTests
{
    private readonly FakeInvoiceFileStore _store = new();

    private readonly IOptions<IntakeOptions> _options =
        Options.Create(new IntakeOptions
        {
            MaxUploadSizeBytes = 1024
        });

    [Fact]
    public async Task ValidPdf_SavesAndReturns201WithoutLocation()
    {
        byte[] bytes = "%PDF-1.4 test"u8.ToArray();
        IFormFile file = CreateFile(bytes, "application/pdf");

        Results<Created<UploadFileResponse>, ValidationProblem> result =
            await UploadInvoiceFile.HandleAsync(
                file,
                _store,
                _options,
                CancellationToken.None);

        Created<UploadFileResponse> created =
            Assert.IsType<Created<UploadFileResponse>>(result.Result);

        Assert.Null(created.Location);
        Assert.NotNull(_store.Saved);
        Assert.Equal(_store.Saved.FileId, created.Value!.FileId);
        Assert.Equal("invoice.pdf", created.Value.FileName);
    }

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public async Task InvalidFile_Returns400ForFileFieldAndSavesNothing(
        byte[] bytes,
        string contentType)
    {
        IFormFile file = CreateFile(bytes, contentType);

        Results<Created<UploadFileResponse>, ValidationProblem> result =
            await UploadInvoiceFile.HandleAsync(
                file,
                _store,
                _options,
                CancellationToken.None);

        ValidationProblem problem =
            Assert.IsType<ValidationProblem>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("file", problem.ProblemDetails.Errors.Keys);
        Assert.Null(_store.Saved);
    }

    public static TheoryData<byte[], string> InvalidFiles =>
        new()
        {
            { [], "application/pdf" },
            { new byte[2048], "application/pdf" },
            { "%PDF-1.4 test"u8.ToArray(), "text/plain" },
            { "hello world"u8.ToArray(), "application/pdf" },
            { "%PD"u8.ToArray(), "application/pdf" }
        };

    private static IFormFile CreateFile(
        byte[] bytes,
        string contentType)
    {
        return new FormFile(
            new MemoryStream(bytes),
            0,
            bytes.Length,
            "file",
            "invoice.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FakeInvoiceFileStore : IInvoiceFileStore
    {
        public StoredFile? Saved { get; private set; }

        public async Task<StoredFile> SaveAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken)
        {
            using MemoryStream buffer = new();

            await content.CopyToAsync(buffer, cancellationToken);

            Saved = new StoredFile(
                Guid.NewGuid(),
                Path.GetFileName(fileName),
                buffer.Length,
                "test-sha256");

            return Saved;
        }

        public Task<bool> ExistsAsync(
            Guid fileId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Saved?.FileId == fileId);
        }
    }
}
