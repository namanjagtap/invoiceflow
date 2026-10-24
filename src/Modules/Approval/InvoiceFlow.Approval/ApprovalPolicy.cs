using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Approval;

internal sealed class ApprovalPolicy(IOptionsMonitor<ApprovalOptions> options) : IApprovalPolicy
{
    public int RequiredApprovers(Money invoiceTotal)
    {
        ArgumentNullException.ThrowIfNull(invoiceTotal);

        if (invoiceTotal.Currency != "INR")
        {
            throw new ArgumentException(
                "Approval policy currently supports INR invoices only.",
                nameof(invoiceTotal));
        }

        ApprovalOptions current = options.CurrentValue;

        foreach (ApprovalTier tier in current.Tiers)
        {
            if (invoiceTotal.Amount <= tier.UpToAmount)
            {
                return tier.RequiredApprovers;
            }
        }

        return current.AboveTopTierApprovers;
    }
}
