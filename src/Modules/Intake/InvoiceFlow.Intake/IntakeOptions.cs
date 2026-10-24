using System.ComponentModel.DataAnnotations;

namespace InvoiceFlow.Intake;

internal sealed class IntakeOptions
{
    public const string SectionName = "Intake";

    [Required]
    public string StoragePath { get; set; } = "";

    [Range(1024, 52_428_800)]       // 1 KB to 50 MB
    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024;
}
