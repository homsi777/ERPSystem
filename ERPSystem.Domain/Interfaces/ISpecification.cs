namespace ERPSystem.Domain.Interfaces;

public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate);
    string FailureReason { get; }
}
