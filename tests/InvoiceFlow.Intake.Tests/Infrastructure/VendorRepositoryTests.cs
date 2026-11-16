using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

[Collection(SqlServerCollection.Name)]
public sealed class VendorRepositoryTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task TryAddAsync_WhenGstinIsUnique_SavesVendorAndPreservesUtc()
    {
        Vendor vendor = TestData.NewVendor();

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new VendorRepository(dbContext);

            Assert.True(
                await repository.TryAddAsync(vendor, CancellationToken.None));
        }

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new VendorRepository(dbContext);

            Vendor? savedVendor = await repository.GetByIdAsync(
                vendor.Id,
                CancellationToken.None);

            Assert.NotNull(savedVendor);
            Assert.Equal(vendor.Id, savedVendor.Id);
            Assert.Equal(vendor.Gstin, savedVendor.Gstin);
            Assert.Equal(DateTimeKind.Utc, savedVendor.RegisteredAtUtc.Kind);
        }
    }

    [Fact]
    public async Task TryAddAsync_WhenGstinAlreadyExists_ReturnsFalse()
    {
        Vendor firstVendor = TestData.NewVendor();

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new VendorRepository(dbContext);

            Assert.True(
                await repository.TryAddAsync(firstVendor, CancellationToken.None));
        }

        Vendor duplicateVendor = Vendor.Register(
            "Duplicate Test Vendor",
            firstVendor.Gstin,
            30,
            TestData.UtcNow);

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new VendorRepository(dbContext);

            Assert.False(
                await repository.TryAddAsync(
                    duplicateVendor,
                    CancellationToken.None));
        }
    }
}
