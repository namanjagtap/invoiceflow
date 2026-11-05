using InvoiceFlow.SharedKernel.Security;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Authorization;

internal static class VendorAccess
{
    public static bool CanActFor(ClaimsPrincipal user, Guid vendorId)
    {
        if (!user.IsInRole(AppRoles.Vendor))
        {
            return true;
        }

        string? vendorIdClaim = user.FindFirst(AppClaims.VendorId)?.Value;

        return Guid.TryParse(vendorIdClaim, out Guid claimedVendorId)
            && claimedVendorId == vendorId;
    }

    public static bool TryResolveVendorScope(
        ClaimsPrincipal user,
        Guid? requestedVendorId,
        out Guid? vendorId)
    {
        vendorId = null;

        if (!user.IsInRole(AppRoles.Vendor))
        {
            vendorId = requestedVendorId;
            return true;
        }

        string? vendorIdClaim = user.FindFirst(AppClaims.VendorId)?.Value;

        if (!Guid.TryParse(vendorIdClaim, out Guid claimedVendorId))
        {
            return false;
        }

        if (requestedVendorId.HasValue && requestedVendorId.Value != claimedVendorId)
        {
            return false;
        }

        vendorId = claimedVendorId;
        return true;
    }
}
