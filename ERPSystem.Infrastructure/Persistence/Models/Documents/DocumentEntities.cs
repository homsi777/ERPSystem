namespace ERPSystem.Infrastructure.Persistence.Models.Documents;

public class DocumentCounterEntity
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string DocumentType { get; set; } = "";
    public string Prefix { get; set; } = "";
    public long LastNumber { get; set; }
    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
