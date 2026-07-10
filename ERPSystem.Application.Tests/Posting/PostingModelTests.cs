using System.Collections.Concurrent;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Posting;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.Posting;

public sealed class PostingModelTests
{
    [Fact]
    public void PostingResult_Succeeded_sets_expected_fields()
    {
        var id = Guid.NewGuid();
        var result = PostingResult.Succeeded(id, "JE-001", Guid.NewGuid(), alreadyPosted: true);

        Assert.True(result.Success);
        Assert.True(result.AlreadyPosted);
        Assert.Equal(id, result.JournalEntryId);
        Assert.Equal("JE-001", result.JournalEntryNumber);
    }

    [Fact]
    public void China_container_posting_kinds_are_distinct()
    {
        Assert.NotEqual(PostingKind.ChinaContainerLandingCost, PostingKind.ChinaContainerInventoryActivation);
    }

    [Fact]
    public void ReversalResult_NotImplemented_does_not_claim_success()
    {
        var result = ReversalResult.NotImplemented();
        Assert.False(result.Success);
        Assert.Equal("reversal_not_implemented", result.ErrorCode);
    }
}

public sealed class WpfPostingGuardTests
{
    [Fact]
    public void ConcurrentDictionary_prevents_duplicate_inflight_key()
    {
        var gate = new ConcurrentDictionary<Guid, byte>();
        var id = Guid.NewGuid();
        Assert.True(gate.TryAdd(id, 0));
        Assert.False(gate.TryAdd(id, 0));
        gate.TryRemove(id, out _);
        Assert.True(gate.TryAdd(id, 0));
    }
}
