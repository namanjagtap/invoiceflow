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

    [Fact]
    public void TryResolveVendorScope_ClerkWithoutRequestedVendor_ReturnsTrueWithNullScope()
    {
        ClaimsPrincipal user = CreateUser(AppRoles.ApClerk);

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            null,
            out Guid? vendorId);

        Assert.True(result);
        Assert.Null(vendorId);
    }

    [Fact]
    public void TryResolveVendorScope_ClerkWithRequestedVendor_ReturnsTrueWithRequestedVendor()
    {
        Guid requestedVendorId = Guid.NewGuid();
        ClaimsPrincipal user = CreateUser(AppRoles.ApClerk);

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            requestedVendorId,
            out Guid? vendorId);

        Assert.True(result);
        Assert.Equal(requestedVendorId, vendorId);
    }

    [Fact]
    public void TryResolveVendorScope_VendorWithoutRequestedVendor_ReturnsOwnVendor()
    {
        Guid ownVendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, ownVendorId.ToString()));

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            null,
            out Guid? vendorId);

        Assert.True(result);
        Assert.Equal(ownVendorId, vendorId);
    }

    [Fact]
    public void TryResolveVendorScope_VendorRequestingOwnVendor_ReturnsTrue()
    {
        Guid ownVendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, ownVendorId.ToString()));

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            ownVendorId,
            out Guid? vendorId);

        Assert.True(result);
        Assert.Equal(ownVendorId, vendorId);
    }

    [Fact]
    public void TryResolveVendorScope_VendorRequestingAnotherVendor_ReturnsFalse()
    {
        Guid ownVendorId = Guid.NewGuid();
        Guid anotherVendorId = Guid.NewGuid();

        ClaimsPrincipal user = CreateUser(
            AppRoles.Vendor,
            new Claim(AppClaims.VendorId, ownVendorId.ToString()));

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            anotherVendorId,
            out Guid? vendorId);

        Assert.False(result);
        Assert.Null(vendorId);
    }

    [Fact]
    public void TryResolveVendorScope_VendorWithoutVendorIdClaim_ReturnsFalse()
    {
        ClaimsPrincipal user = CreateUser(AppRoles.Vendor);

        bool result = VendorAccess.TryResolveVendorScope(
            user,
            null,
            out Guid? vendorId);

        Assert.False(result);
        Assert.Null(vendorId);
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
