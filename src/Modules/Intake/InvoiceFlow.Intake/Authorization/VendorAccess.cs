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
}
