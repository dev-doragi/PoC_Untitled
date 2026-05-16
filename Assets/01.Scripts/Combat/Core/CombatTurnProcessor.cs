using UnityEngine;

/// <summary>
/// Applies turn-end updates for hourglass combat state.
/// </summary>
public class CombatTurnProcessor
{
    public void EndTurn(CombatRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.IsCombatEnded)
        {
            return;
        }

        CombatActorRuntime endingActor = runtimeState.GetActor(runtimeState.TurnState);
        CombatActorRuntime receivingActor = runtimeState.GetOpponent(runtimeState.TurnState);
        if (endingActor == null || receivingActor == null)
        {
            return;
        }

        int transferAmount = endingActor.TransferredSand + runtimeState.FlipTransfer;
        bool applyGroggyReduction = receivingActor.GroggyActive || receivingActor.GroggyPending;
        int received = receivingActor.ReceiveSand(transferAmount, applyGroggyReduction, runtimeState.GroggyIncomingSandMultiplier);
        int clamped = Mathf.Clamp(
            received,
            Mathf.Max(1, runtimeState.MinimumTurnSand),
            Mathf.Max(1, runtimeState.MaxTransferSand));
        receivingActor.AvailableSand = clamped;
        endingActor.AvailableSand = 0;
        endingActor.ConsumeTurnSand();

        if (receivingActor.GroggyPending)
        {
            receivingActor.GroggyPending = false;
            receivingActor.GroggyActive = true;
        }
        if (endingActor.GroggyActive)
        {
            endingActor.GroggyActive = false;
            if (endingActor.ActorType == CombatActorType.Enemy)
            {
                endingActor.EnemyGuard = Mathf.Max(1, endingActor.MaxEnemyGuard);
            }
        }

        if (endingActor.ActorType == CombatActorType.Enemy && receivingActor.ActorType == CombatActorType.Player)
        {
            receivingActor.GuardValue = 0;
        }

        runtimeState.TurnIndex += 1;
        runtimeState.TurnState = runtimeState.TurnState == CombatTurnState.PlayerTurn
            ? CombatTurnState.EnemyTurn
            : CombatTurnState.PlayerTurn;

        if (runtimeState.Player.IsDead || runtimeState.Enemy.IsDead)
        {
            runtimeState.IsCombatEnded = true;
            runtimeState.TurnState = CombatTurnState.Ended;
        }
    }
}
