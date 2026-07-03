using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.System;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class ContainerAuditRecorder
{
    public const string EntityTypeName = "ChinaContainer";

    public static async Task RecordStatusChangeAsync(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUser,
        ICurrentBranchService branch,
        Guid containerId,
        string action,
        ChinaContainerStatus previousStatus,
        ChinaContainerStatus newStatus,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var oldValues = $"{{\"status\":\"{previousStatus}\"}}";
        var newValues = notes is null
            ? $"{{\"status\":\"{newStatus}\"}}"
            : $"{{\"status\":\"{newStatus}\",\"notes\":\"{notes}\"}}";

        await auditLogRepository.AddAsync(
            AuditLog.Record(
                currentUser.UserId,
                action,
                EntityTypeName,
                containerId,
                oldValues,
                newValues,
                branch.BranchId),
            cancellationToken);
    }
}
