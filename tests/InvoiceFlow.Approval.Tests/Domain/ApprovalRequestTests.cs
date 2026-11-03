using InvoiceFlow.Approval.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Approval.Tests.Domain;

public sealed class ApprovalRequestTests
{
    private static readonly DateTime UtcNow =
        new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithEmptyInvoiceId_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ApprovalRequest.Create(
                Guid.Empty,
                Money.Inr(1000m),
                1,
                UtcNow));

        Assert.Equal("invoiceId", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullAmount_ThrowsArgumentNullException()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => ApprovalRequest.Create(
                    Guid.NewGuid(),
                    null!,
                    1,
                    UtcNow));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ApprovalRequest.Create(
                    Guid.NewGuid(),
                    Money.Inr(0m),
                    1,
                    UtcNow));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Create_WithZeroRequiredApprovals_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ApprovalRequest.Create(
                    Guid.NewGuid(),
                    Money.Inr(1000m),
                    0,
                    UtcNow));

        Assert.Equal("requiredApprovals", exception.ParamName);
    }

    [Fact]
    public void Create_WithNonUtcDateTime_ThrowsArgumentException()
    {
        DateTime nonUtcTime = new(
            2026, 11, 2, 10, 0, 0, DateTimeKind.Unspecified);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ApprovalRequest.Create(
                Guid.NewGuid(),
                Money.Inr(1000m),
                1,
                nonUtcTime));

        Assert.Equal("utcNow", exception.ParamName);
    }

    [Fact]
    public void Create_WithValidData_CreatesPendingRequest()
    {
        ApprovalRequest request = CreateRequest();

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.Equal(1, request.RequiredApprovals);
        Assert.Empty(request.Decisions);
    }

    [Fact]
    public void Approve_WithOneRequiredApproval_MarksRequestApproved()
    {
        ApprovalRequest request = CreateRequest(requiredApprovals: 1);

        request.Approve("approver-1", UtcNow);

        Assert.Equal(ApprovalStatus.Approved, request.Status);
        Assert.Single(request.Decisions);
        Assert.Equal("approver-1", request.Decisions[0].ApproverId);
    }

    [Fact]
    public void Approve_WithTwoRequiredApprovals_RemainsPendingAfterFirstApproval()
    {
        ApprovalRequest request = CreateRequest(requiredApprovals: 2);

        request.Approve("approver-1", UtcNow);

        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.Single(request.Decisions);
    }

    [Fact]
    public void Approve_WithTwoDifferentApprovers_MarksRequestApproved()
    {
        ApprovalRequest request = CreateRequest(requiredApprovals: 2);

        request.Approve("approver-1", UtcNow);
        request.Approve("approver-2", UtcNow.AddMinutes(5));

        Assert.Equal(ApprovalStatus.Approved, request.Status);
        Assert.Equal(2, request.Decisions.Count);
    }

    [Fact]
    public void Approve_WithSameApproverTwice_ThrowsInvalidOperationException()
    {
        ApprovalRequest request = CreateRequest(requiredApprovals: 2);

        request.Approve("approver-1", UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Approve("approver-1", UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperationException()
    {
        ApprovalRequest request = CreateRequest(requiredApprovals: 1);

        request.Approve("approver-1", UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Approve("approver-2", UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void Approve_WithBlankApproverId_ThrowsArgumentException()
    {
        ApprovalRequest request = CreateRequest();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => request.Approve(" ", UtcNow));

        Assert.Equal("approverId", exception.ParamName);
    }

    [Fact]
    public void Approve_WithNonUtcDateTime_ThrowsArgumentException()
    {
        ApprovalRequest request = CreateRequest();

        DateTime nonUtcTime = new(
            2026, 11, 2, 10, 0, 0, DateTimeKind.Unspecified);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => request.Approve("approver-1", nonUtcTime));

        Assert.Equal("utcNow", exception.ParamName);
    }

    private static ApprovalRequest CreateRequest(
        int requiredApprovals = 1)
    {
        return ApprovalRequest.Create(
            Guid.NewGuid(),
            Money.Inr(50000m),
            requiredApprovals,
            UtcNow);
    }
}
