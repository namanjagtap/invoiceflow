using InvoiceFlow.Approval.Authorization;
using InvoiceFlow.Approval.Domain;
using InvoiceFlow.Approval.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InvoiceFlow.Approval.Endpoints;

internal static class ApprovalRequestEndpoints
{
    internal static RouteGroupBuilder MapApprovalRequestEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateRequest)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(ApprovalPolicies.CreateRequests);

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetApprovalRequestById")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(ApprovalPolicies.ReadRequests);

        group.MapPost("/{id:guid}/approve", Approve)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(ApprovalPolicies.Approve);

        return group;
    }

    private static Results<CreatedAtRoute<ApprovalRequestResponse>, ValidationProblem, ProblemHttpResult> CreateRequest(
        CreateApprovalRequestRequest request,
        IApprovalPolicy approvalPolicy,
        IApprovalRequestRepository repository,
        TimeProvider timeProvider)
    {
        Dictionary<string, string[]> errors = [];

        if (request.InvoiceId == Guid.Empty)
        {
            errors[nameof(request.InvoiceId)] = ["Invoice ID is required."];
        }

        if (request.Amount is null or <= 0)
        {
            errors[nameof(request.Amount)] = ["Amount must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        Money amount = Money.Inr(request.Amount!.Value);

        int requiredApprovals = approvalPolicy.RequiredApprovers(amount);

        ApprovalRequest approvalRequest = ApprovalRequest.Create(
            request.InvoiceId,
            amount,
            requiredApprovals,
            timeProvider.GetUtcNow().UtcDateTime);

        if (!repository.TryAdd(approvalRequest))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate approval request",
                detail: "An approval request already exists for this invoice.");
        }

        ApprovalRequestResponse response = MapResponse(approvalRequest);

        return TypedResults.CreatedAtRoute(
            response,
            "GetApprovalRequestById",
            new { id = approvalRequest.Id });
    }

    private static Results<Ok<ApprovalRequestResponse>, ProblemHttpResult> GetById(
        Guid id,
        IApprovalRequestRepository repository)
    {
        ApprovalRequest? request = repository.GetById(id);

        if (request is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Approval request not found");
        }

        return TypedResults.Ok(MapResponse(request));
    }

    private static async Task<Results<Ok<ApprovalRequestResponse>, ProblemHttpResult, ForbidHttpResult>> Approve(
        Guid id,
        ClaimsPrincipal user,
        IApprovalRequestRepository repository,
        IAuthorizationService authorizationService,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("InvoiceFlow.Approval.Requests");

        ApprovalRequest? request = repository.GetById(id);

        if (request is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Approval request not found");
        }

        string? approverId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(approverId))
        {
            return TypedResults.Forbid();
        }

        AuthorizationResult authorizationResult = await authorizationService.AuthorizeAsync(
            user,
            request,
            ApprovalPolicies.WithinApprovalLimit);

        if (!authorizationResult.Succeeded)
        {
            logger.LogWarning(
                "Approver {ApproverId} denied approval request {ApprovalRequestId} for {Amount}",
                approverId,
                request.Id,
                request.Amount.Amount);

            return TypedResults.Forbid();
        }

        if (request.Status == ApprovalStatus.Approved)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Approval request already approved");
        }

        if (request.HasApproved(approverId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Already approved by this approver");
        }

        request.Approve(
            approverId,
            timeProvider.GetUtcNow().UtcDateTime);

        logger.LogInformation(
            "Approval request {ApprovalRequestId} approved by {ApproverId} ({ApprovalCount}/{RequiredApprovals})",
            request.Id,
            approverId,
            request.Decisions.Count,
            request.RequiredApprovals);

        return TypedResults.Ok(MapResponse(request));
    }

    private static ApprovalRequestResponse MapResponse(
        ApprovalRequest request)
    {
        return new ApprovalRequestResponse(
            request.Id,
            request.InvoiceId,
            request.Amount.Amount,
            request.Status.ToString(),
            request.RequiredApprovals,
            request.Decisions
                .Select(decision => decision.ApproverId)
                .ToList());
    }
}
