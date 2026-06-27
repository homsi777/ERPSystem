using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.ChinaImport;

public sealed record LandingCostCalculated(Guid ContainerId, string ContainerNumber) : DomainEvent;

public sealed record ContainerApproved(Guid ContainerId, string ContainerNumber) : DomainEvent;

public sealed record ContainerMovedToWarehouse(Guid ContainerId, string ContainerNumber) : DomainEvent;
