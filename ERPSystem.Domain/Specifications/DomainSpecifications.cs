using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Interfaces;

namespace ERPSystem.Domain.Specifications;

public sealed class InvoiceCanBeApprovedSpecification : ISpecification<SalesInvoiceAggregate>
{
    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(SalesInvoiceAggregate candidate)
    {
        if (candidate.Status is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
        {
            FailureReason = "Invoice must be detailed or ready for approval.";
            return false;
        }

        if (candidate.RollDetails.Any(d => !d.HasValidLength))
        {
            FailureReason = "All roll lengths must be valid.";
            return false;
        }

        if (candidate.GrandTotal.Amount <= 0)
        {
            FailureReason = "Invoice total must be greater than zero.";
            return false;
        }

        return true;
    }
}

public sealed class ContainerCanBeApprovedSpecification : ISpecification<ContainerAggregate>
{
    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(ContainerAggregate candidate)
    {
        if (candidate.LandingCost is null || candidate.LandingCost.Status != LandingCostStatus.Reviewed)
        {
            FailureReason = "Landing cost must be reviewed.";
            return false;
        }

        if (candidate.Items.Any(i => !i.IsValid))
        {
            FailureReason = "All container items must be valid.";
            return false;
        }

        if (candidate.TotalMeters.Value <= 0)
        {
            FailureReason = "Container must have total meters greater than zero.";
            return false;
        }

        return true;
    }
}

public sealed class WarehouseCanDetailSpecification : ISpecification<SalesInvoiceAggregate>
{
    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(SalesInvoiceAggregate candidate)
    {
        if (candidate.Status != SalesInvoiceStatus.AwaitingDetailing)
        {
            FailureReason = "الفاتورة ليست بانتظار التفصيل — حدّث الصفحة للتحقق من الحالة.";
            return false;
        }

        if (candidate.Items.Count == 0)
        {
            FailureReason = "الفاتورة لا تحتوي أصنافاً.";
            return false;
        }

        return true;
    }
}

public sealed class CreditLimitSatisfiedSpecification : ISpecification<CustomerAggregate>
{
    private readonly decimal _additionalAmount;

    public CreditLimitSatisfiedSpecification(decimal additionalAmount) =>
        _additionalAmount = additionalAmount;

    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(CustomerAggregate candidate)
    {
        if (candidate.WouldExceedCreditLimit(_additionalAmount))
        {
            FailureReason = "Credit limit would be exceeded.";
            return false;
        }

        return true;
    }
}

public sealed class BalancedJournalSpecification : ISpecification<AccountingAggregate>
{
    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(AccountingAggregate candidate)
    {
        if (candidate.Lines.Count == 0)
        {
            FailureReason = "Journal entry must have lines.";
            return false;
        }

        if (Math.Abs(candidate.DebitTotal.Amount - candidate.CreditTotal.Amount) > 0.01m)
        {
            FailureReason = "Debits and credits must balance.";
            return false;
        }

        return true;
    }
}

public sealed class LandingCostValidSpecification : ISpecification<ContainerAggregate>
{
    public string FailureReason { get; private set; } = "";

    public bool IsSatisfiedBy(ContainerAggregate candidate)
    {
        if (candidate.LandingCost is null)
        {
            FailureReason = "Landing cost has not been calculated.";
            return false;
        }

        if (candidate.TotalMeters.Value <= 0)
        {
            FailureReason = "Total meters must be greater than zero.";
            return false;
        }

        if (candidate.LandingCost.TotalImportExpenses.Amount < 0)
        {
            FailureReason = "Total import expenses cannot be negative.";
            return false;
        }

        return true;
    }
}
