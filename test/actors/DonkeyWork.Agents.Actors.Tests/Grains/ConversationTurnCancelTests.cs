using System.Threading.Channels;
using DonkeyWork.Agents.Actors.Contracts.Models;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Grains;

public class ConversationTurnCancelTests
{
    #region CancelTurnAsync Tests

    [Fact]
    public void CancelTurn_WhenTurnIsActive_CancelsAndReturnsActive()
    {
        var logic = new TurnCancelLogic();
        var turnId = Guid.NewGuid();
        logic.StartTurn(turnId);

        var result = logic.CancelTurn(turnId);

        Assert.Equal(CancelTurnResult.Active, result);
        Assert.True(logic.IsCurrentTurnCancelled);
    }

    [Fact]
    public void CancelTurn_WhenTurnIsPending_TombstonesAndReturnsPending()
    {
        var logic = new TurnCancelLogic();
        var turnId = Guid.NewGuid();

        var result = logic.CancelTurn(turnId);

        Assert.Equal(CancelTurnResult.Pending, result);
        Assert.True(logic.IsTombstoned(turnId));
    }

    [Fact]
    public void CancelTurn_WhenAlreadyTombstoned_ReturnsNotFound()
    {
        var logic = new TurnCancelLogic();
        var turnId = Guid.NewGuid();
        logic.CancelTurn(turnId);

        var result = logic.CancelTurn(turnId);

        Assert.Equal(CancelTurnResult.NotFound, result);
    }

    [Fact]
    public void CancelTurn_WhenTurnCompletedAndNotYetTombstoned_ReturnsPending()
    {
        var logic = new TurnCancelLogic();
        var turnId = Guid.NewGuid();
        logic.StartTurn(turnId);
        logic.EndTurn();

        var result = logic.CancelTurn(turnId);

        Assert.Equal(CancelTurnResult.Pending, result);
    }

    [Fact]
    public void CancelTurn_DifferentActiveTurn_TombstonesAndReturnsPending()
    {
        var logic = new TurnCancelLogic();
        var activeTurnId = Guid.NewGuid();
        var pendingTurnId = Guid.NewGuid();
        logic.StartTurn(activeTurnId);

        var result = logic.CancelTurn(pendingTurnId);

        Assert.Equal(CancelTurnResult.Pending, result);
        Assert.False(logic.IsCurrentTurnCancelled);
        Assert.True(logic.IsTombstoned(pendingTurnId));
    }

    #endregion

    #region Queue Skip Tests

    [Fact]
    public async Task ProcessQueue_MiddleTurnTombstoned_IsSkipped()
    {
        var logic = new TurnCancelLogic();
        var turn1 = Guid.NewGuid();
        var turn2 = Guid.NewGuid();
        var turn3 = Guid.NewGuid();

        var channel = Channel.CreateUnbounded<UserConversationMessage>();
        channel.Writer.TryWrite(new UserConversationMessage("msg1", turn1, DateTimeOffset.UtcNow));
        channel.Writer.TryWrite(new UserConversationMessage("msg2", turn2, DateTimeOffset.UtcNow));
        channel.Writer.TryWrite(new UserConversationMessage("msg3", turn3, DateTimeOffset.UtcNow));
        channel.Writer.Complete();

        logic.CancelTurn(turn2);

        var processed = new List<string>();
        await foreach (var msg in channel.Reader.ReadAllAsync())
        {
            if (logic.ShouldSkip(msg.TurnId))
                continue;
            processed.Add(msg.Text);
        }

        Assert.Equal(2, processed.Count);
        Assert.Equal("msg1", processed[0]);
        Assert.Equal("msg3", processed[1]);
    }

    [Fact]
    public async Task ProcessQueue_TombstoneIsConsumedOnSkip()
    {
        var logic = new TurnCancelLogic();
        var turnId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<UserConversationMessage>();
        channel.Writer.TryWrite(new UserConversationMessage("msg", turnId, DateTimeOffset.UtcNow));
        channel.Writer.Complete();

        logic.CancelTurn(turnId);

        Assert.True(logic.IsTombstoned(turnId));

        await foreach (var msg in channel.Reader.ReadAllAsync())
        {
            logic.ShouldSkip(msg.TurnId);
        }

        Assert.False(logic.IsTombstoned(turnId));
    }

    #endregion

    #region Helper

    private sealed class TurnCancelLogic
    {
        private readonly HashSet<Guid> _cancelledTurnIds = new();
        private CancellationTokenSource? _currentTurnCts;
        private Guid? _currentTurnId;

        public bool IsCurrentTurnCancelled => _currentTurnCts?.IsCancellationRequested ?? false;

        public void StartTurn(Guid turnId)
        {
            _currentTurnCts = new CancellationTokenSource();
            _currentTurnId = turnId;
        }

        public void EndTurn()
        {
            _currentTurnCts?.Dispose();
            _currentTurnCts = null;
            _currentTurnId = null;
        }

        public CancelTurnResult CancelTurn(Guid turnId)
        {
            if (turnId == _currentTurnId)
            {
                _currentTurnCts?.Cancel();
                return CancelTurnResult.Active;
            }

            if (_cancelledTurnIds.Contains(turnId))
                return CancelTurnResult.NotFound;

            _cancelledTurnIds.Add(turnId);
            return CancelTurnResult.Pending;
        }

        public bool ShouldSkip(Guid turnId) => _cancelledTurnIds.Remove(turnId);

        public bool IsTombstoned(Guid turnId) => _cancelledTurnIds.Contains(turnId);
    }

    #endregion
}
