namespace FragranceCatalog.Application.Alerts;

/// <summary>Everything that needs the administrator's attention about prices.</summary>
/// <param name="LowProfit">Products earning less than the minimum.</param>
/// <param name="CostChanges">Products whose cost moved more than the alert percentage in the last sync.</param>
public sealed record PricingAlertsDto(IReadOnlyList<LowProfitAlertDto> LowProfit, IReadOnlyList<CostChangeAlertDto> CostChanges);
