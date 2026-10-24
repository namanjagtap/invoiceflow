using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Approval.Tests;

public class ApprovalPolicyTests
{
    private static ServiceProvider CreateProvider(
        decimal firstTier = 100_000m,
        decimal secondTier = 1_000_000m,
        int firstApprovers = 1,
        int secondApprovers = 2,
        int aboveTopTierApprovers = 3)
    {
        Dictionary<string, string?> settings = new()
        {
            ["Approval:Tiers:0:UpToAmount"] = firstTier.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["Approval:Tiers:0:RequiredApprovers"] = firstApprovers.ToString(),
            ["Approval:Tiers:1:UpToAmount"] = secondTier.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["Approval:Tiers:1:RequiredApprovers"] = secondApprovers.ToString(),
            ["Approval:AboveTopTierApprovers"] = aboveTopTierApprovers.ToString()
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddApprovalModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
    }

    public static TheoryData<decimal, int> TierCases => new()
    {
        { 50_000m, 1 },
        { 100_000m, 1 },
        { 100_000.01m, 2 },
        { 1_000_000m, 2 },
        { 1_000_000.01m, 3 }
    };
    [Theory]
    [MemberData(nameof(TierCases))]
    public void RequiredApprovers_ReturnsExpectedTier(
        decimal amount,
        int expectedApprovers)
    {
        using ServiceProvider provider = CreateProvider();
        IApprovalPolicy policy =
            provider.GetRequiredService<IApprovalPolicy>();

        int actual = policy.RequiredApprovers(Money.Inr(amount));

        Assert.Equal(expectedApprovers, actual);
    }

    [Fact]
    public void RequiredApprovers_RejectsNonInrCurrency()
    {
        using ServiceProvider provider = CreateProvider();
        IApprovalPolicy policy =
            provider.GetRequiredService<IApprovalPolicy>();

        Assert.Throws<ArgumentException>(
            () => policy.RequiredApprovers(Money.Create(100m, "USD")));
    }

    [Fact]
    public void InvalidConfiguration_NoTiers_ThrowsValidationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Approval:AboveTopTierApprovers"] = "3"
                })
            .Build();

        ServiceCollection services = new();
        services.AddApprovalModule(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApprovalOptions>>().Value);

        Assert.Contains("At least one approval tier", ex.Message);
    }

    [Fact]
    public void InvalidConfiguration_DescendingTiers_ThrowsValidationException()
    {
        using ServiceProvider provider = CreateProvider(
            firstTier: 1_000_000m,
            secondTier: 100_000m);

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApprovalOptions>>().Value);

        Assert.Contains("strictly ascending", ex.Message);
    }
}