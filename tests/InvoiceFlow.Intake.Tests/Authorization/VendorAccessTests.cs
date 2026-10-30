using InvoiceFlow.Intake.Authorization;
using InvoiceFlow.SharedKernel.Security;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Tests.Authorization;

public sealed class VendorAccessTests
{
    [Fact]
    public void CanActFor_ClerkActingForAnyVendor_ReturnsTrue()
    {
        Guid requestedVendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(AppRoles.ApClerk);

        bool result = VendorAccess.CanActFor(user, requestedVendorId);

        Assert.True(result);
    }

    [Fact]
    public void CanActFor_VendorActingForOwnVendor_ReturnsTrue()
    {
        Guid vendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, vendorId.ToString()));

        bool result = VendorAccess.CanActFor(user, vendorId);

        Assert.True(result);
    }

    [Fact]
    public void CanActFor_VendorActingForAnotherVendor_ReturnsFalse()
    {
        Guid ownVendorId = Guid.NewGuid();
        Guid anotherVendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, ownVendorId.ToString()));

        bool result = VendorAccess.CanActFor(user, anotherVendorId);

        Assert.False(result);
    }

    [Fact]
    public void CanActFor_VendorWithoutVendorIdClaim_ReturnsFalse()
    {
        Guid vendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(AppRoles.Vendor);

        bool result = VendorAccess.CanActFor(user, vendorId);

        Assert.False(result);
    }

    [Fact]
    public void CanActFor_VendorWithMalformedVendorIdClaim_ReturnsFalse()
    {
        Guid vendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, "not-a-guid"));

        bool result = VendorAccess.CanActFor(user, vendorId);

        Assert.False(result);
    }

    private static ClaimsPrincipal CreateUser(
        string role,
        params Claim[] additionalClaims)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.Role, role),
            .. additionalClaims
        ];

        ClaimsIdentity identity = new(claims, "Test");

        return new ClaimsPrincipal(identity);
    }
}
