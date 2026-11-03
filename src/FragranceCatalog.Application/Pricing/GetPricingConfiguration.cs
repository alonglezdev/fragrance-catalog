namespace FragranceCatalog.Application.Pricing;

/// <summary>Shows the administrator the current pricing configuration.</summary>
/// <param name="reader">Pricing data.</param>
public sealed class GetPricingConfiguration(IPricingReader reader)
{
    /// <summary>Reads the settings, ranges and rules.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    public async Task<PricingConfigurationDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var configuration = await reader.GetConfigurationAsync(cancellationToken);

        return new PricingConfigurationDto(
            PricingSettingsDto.From(configuration.Settings),
            configuration.Ranges.OrderBy(range => range.MinCost).Select(PriceRangeDto.From).ToList(),
            configuration.Rules.Select(PricingRuleDto.From).ToList());
    }
}
