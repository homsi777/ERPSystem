using ERPSystem.Domain.Entities.Capital;

namespace ERPSystem.Domain.Aggregates;

public sealed class CapitalPartnerAggregate : Common.AggregateRoot
{
    public CapitalPartner Partner { get; private set; } = null!;

    private CapitalPartnerAggregate() { }

    public static CapitalPartnerAggregate FromPartner(CapitalPartner partner) => new()
    {
        Id = partner.Id,
        Partner = partner
    };
}
