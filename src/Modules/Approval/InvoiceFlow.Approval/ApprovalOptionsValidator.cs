using Microsoft.Extensions.Options;

namespace InvoiceFlow.Approval;

internal sealed class ApprovalOptionsValidator : IValidateOptions<ApprovalOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        ApprovalOptions options)
    {
        List<string> failures = [];

        if (options.Tiers.Count == 0)
        {
            failures.Add("At least one approval tier must be configured.");
        }

        decimal? previousAmount = null;
        int previousApprovers = 0;

        for (int i = 0; i < options.Tiers.Count; i++)
        {
            ApprovalTier tier = options.Tiers[i];

            if (tier.UpToAmount <= 0)
            {
                failures.Add($"Tier {i + 1}: UpToAmount must be greater than zero.");
            }
            if (tier.RequiredApprovers < 1)
            {
                failures.Add($"Tier {i + 1}: RequiredApprovers must be at least 1.");
            }
            if (previousAmount.HasValue && tier.UpToAmount <= previousAmount.Value)
            {
                failures.Add($"Tier {i + 1}: UpToAmount values must be strictly ascending.");
            }
            if (i > 0 && tier.RequiredApprovers < previousApprovers)
            {
                failures.Add($"Tier {i + 1}: RequiredApprovers must not decrease.");
            }

            previousAmount = tier.UpToAmount;
            previousApprovers = tier.RequiredApprovers;
        }

        if (options.AboveTopTierApprovers < 1)
        {
            failures.Add("AboveTopTierApprovers must be at least 1.");
        }

        if (options.Tiers.Count > 0 && options.AboveTopTierApprovers < options.Tiers[^1].RequiredApprovers)
        {
            failures.Add("AboveTopTierApprovers must be greater than or equal to the last tier's RequiredApprovers.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
