using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Approval;

internal interface IApprovalPolicy
{
    int RequiredApprovers(Money invoiceTotal);
}
