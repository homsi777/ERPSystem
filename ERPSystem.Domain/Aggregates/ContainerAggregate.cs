using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Events.ChinaImport;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Aggregates;

public sealed class ContainerAggregate : AggregateRoot
{
    public ContainerNumber ContainerNumber { get; private set; } = null!;
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid? ChinaOrderId { get; private set; }
    public ChinaContainerStatus Status { get; private set; }
    public DateTime ShipmentDate { get; private set; }
    public DateTime? ExpectedArrival { get; private set; }
    public DateTime? ArrivalDate { get; private set; }
    public int TotalRolls { get; private set; }
    public LengthInMeters TotalMeters { get; private set; } = LengthInMeters.Zero;
    public WeightInKg? TotalWeight { get; private set; }
    public string? Port { get; private set; }
    public string? Notes { get; private set; }
    public decimal ExchangeRateToLocalCurrency { get; private set; } = 1m;
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<ChinaContainerItem> _items = [];
    private readonly List<ChinaImportBatch> _importBatches = [];
    private readonly List<ContainerCustomerDistribution> _distributions = [];
    private LandingCost? _landingCost;

    public IReadOnlyList<ChinaContainerItem> Items => _items.AsReadOnly();
    public IReadOnlyList<ChinaImportBatch> ImportBatches => _importBatches.AsReadOnly();
    public IReadOnlyList<ContainerCustomerDistribution> Distributions => _distributions.AsReadOnly();
    public LandingCost? LandingCost => _landingCost;

    private ContainerAggregate() { }

    public static ContainerAggregate CreateDraft(
        ContainerNumber containerNumber,
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        DateTime shipmentDate)
    {
        return new ContainerAggregate
        {
            ContainerNumber = containerNumber,
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplierId,
            ShipmentDate = shipmentDate,
            Status = ChinaContainerStatus.Draft
        };
    }

    public void SetHeaderDetails(
        DateTime? expectedArrival,
        string? notes,
        decimal exchangeRateToLocalCurrency)
    {
        if (exchangeRateToLocalCurrency <= 0)
            throw new ValidationException("Exchange rate must be greater than zero.");
        ExpectedArrival = expectedArrival;
        Notes = notes;
        ExchangeRateToLocalCurrency = exchangeRateToLocalCurrency;
    }

    public void MarkInTransit() => TransitionTo(ChinaContainerStatus.InTransit);
    public void MarkArrived(DateTime arrivalDate)
    {
        ArrivalDate = arrivalDate;
        TransitionTo(ChinaContainerStatus.Arrived);
    }

    public void AddImportBatch(ChinaImportBatch batch) => _importBatches.Add(batch);

    public void AddItem(ChinaContainerItem item)
    {
        if (Status is ChinaContainerStatus.Approved or ChinaContainerStatus.InWarehouse or ChinaContainerStatus.Closed)
            throw new ContainerApprovalException("Cannot modify items on approved container.");
        _items.Add(item);
        RecalculateTotals();
    }

    public void BeginReview()
    {
        if (_items.Any(i => !i.IsValid))
            throw new ContainerApprovalException("Cannot review container with invalid import rows.");
        TransitionTo(ChinaContainerStatus.UnderReview);
    }

    public void SetLandingCost(LandingCost landingCost)
    {
        if (TotalMeters.Value <= 0)
            throw new ContainerApprovalException("Total meters must be greater than zero before landing cost.");
        _landingCost = landingCost;
        _landingCost.MarkReviewed(Guid.Empty);
        Status = ChinaContainerStatus.LandingCostReviewed;
        Raise(new LandingCostCalculated(Id, ContainerNumber.Value));
    }

    public void Approve(Guid userId)
    {
        if (_landingCost is null || _landingCost.Status != LandingCostStatus.Reviewed)
            throw new ContainerApprovalException("Landing cost must be reviewed before container approval.");
        if (_items.Any(i => !i.IsValid))
            throw new ContainerApprovalException("All container items must be valid.");
        _landingCost.Approve();
        Status = ChinaContainerStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = userId;
        Raise(new ContainerApproved(Id, ContainerNumber.Value));
    }

    public void MoveToWarehouse()
    {
        EnsureStatus(ChinaContainerStatus.Approved);
        Status = ChinaContainerStatus.InWarehouse;
        Raise(new ContainerMovedToWarehouse(Id, ContainerNumber.Value));
    }

    public void Close() => TransitionTo(ChinaContainerStatus.Closed);
    public void Archive() { IsArchived = true; Status = ChinaContainerStatus.Archived; }

    public void AddDistribution(ContainerCustomerDistribution distribution) =>
        _distributions.Add(distribution);

    private void RecalculateTotals()
    {
        TotalRolls = _items.Sum(i => i.RollCount);
        var meters = _items.Sum(i => i.LengthMeters.Value);
        TotalMeters = meters > 0 ? new LengthInMeters(meters) : LengthInMeters.Zero;

        var weightSum = _items.Where(i => i.WeightKg is not null).Sum(i => i.WeightKg!.Value);
        TotalWeight = weightSum > 0 ? new WeightInKg(weightSum) : null;
    }

    private void TransitionTo(ChinaContainerStatus newStatus) => Status = newStatus;

    private void EnsureStatus(ChinaContainerStatus required)
    {
        if (Status != required)
            throw new ContainerApprovalException($"Container must be in status '{required}'.");
    }
}
