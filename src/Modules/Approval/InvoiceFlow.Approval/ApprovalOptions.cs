namespace InvoiceFlow.Approval;

internal sealed class ApprovalOptions
{
    public const string SectionName = "Approval";
    public List<ApprovalTier> Tiers { get; set; } = [];
    public int AboveTopTierApprovers { get; set; }
}

internal sealed class ApprovalTier
{
    public decimal UpToAmount { get; set; }
    public int RequiredApprovers { get; set; }
}
