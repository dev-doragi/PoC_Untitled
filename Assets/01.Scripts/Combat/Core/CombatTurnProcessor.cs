using UnityEngine;

/// <summary>
/// Applies turn-end updates for hourglass combat state.
/// </summary>
public class CombatTurnProcessor
{
    public readonly struct TurnTransitionResult
    {
        public readonly bool BonusTurnGranted;
        public readonly CombatActorType BonusActor;
        public readonly int ForcedFallAmount;
        public readonly CombatActorType ForcedFallActor;

        public TurnTransitionResult(bool bonusTurnGranted, CombatActorType bonusActor, int forcedFallAmount, CombatActorType forcedFallActor)
        {
            BonusTurnGranted = bonusTurnGranted;
            BonusActor = bonusActor;
            ForcedFallAmount = forcedFallAmount;
            ForcedFallActor = forcedFallActor;
        }
    }

    public TurnTransitionResult EndTurn(CombatRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.IsCombatEnded)
        {
            return new TurnTransitionResult(false, CombatActorType.None, 0, CombatActorType.None);
        }

        CombatTurnState endingTurnState = runtimeState.TurnState;
        CombatActorRuntime endingActor = runtimeState.GetActor(runtimeState.TurnState);
        CombatActorRuntime receivingActor = runtimeState.GetOpponent(runtimeState.TurnState);
        if (endingActor == null || receivingActor == null)
        {
            return new TurnTransitionResult(false, CombatActorType.None, 0, CombatActorType.None);
        }

        int completedUpper = Mathf.Max(0, runtimeState.UpperSand);
        int completedLower = Mathf.Max(0, runtimeState.LowerSand);
        int totalSand = Mathf.Max(0, runtimeState.TotalSand);
        int forcedFallAmount = 0;
        CombatActorType forcedFallActor = endingActor.ActorType;

        // Apply MinimumFall on the ending actor's buckets before flip.
        int minimumFall = Mathf.Max(0, runtimeState.MinimumFall);
        if (minimumFall > 0 && completedLower < minimumFall && completedUpper > 0)
        {
            int shortage = minimumFall - completedLower;
            forcedFallAmount = Mathf.Min(shortage, completedUpper);
            if (forcedFallAmount > 0)
            {
                completedUpper -= forcedFallAmount;
                completedLower += forcedFallAmount;
            }
        }

        // Flip first: lower becomes next upper, upper becomes next lower.
        int flippedUpper = Mathf.Clamp(completedLower, 0, totalSand);
        int flippedLower = Mathf.Clamp(completedUpper, 0, totalSand);

        runtimeState.UpperSand = flippedUpper;
        runtimeState.LowerSand = flippedLower;

        int spendableAfterFlip = Mathf.Max(0, totalSand - runtimeState.LockedSand - runtimeState.LowerSand);
        runtimeState.UpperSand = Mathf.Min(runtimeState.UpperSand, spendableAfterFlip);

        endingActor.ConsumeTurnSand();
        endingActor.AvailableSand = 0;

        // Flip complete: switch to next actor first, then apply post-flip minimum fall.
        runtimeState.TurnState = runtimeState.TurnState == CombatTurnState.PlayerTurn
            ? CombatTurnState.EnemyTurn
            : CombatTurnState.PlayerTurn;

        CombatActorRuntime nextActor = runtimeState.GetActor(runtimeState.TurnState);
        CombatActorRuntime nextOpponent = runtimeState.GetOpponent(runtimeState.TurnState);

        bool bonusTurnGranted = false;
        CombatActorType bonusActor = CombatActorType.None;
        bool skipEnemyTurn = endingTurnState == CombatTurnState.PlayerTurn
            && nextActor != null
            && nextActor.ActorType == CombatActorType.Enemy
            && (nextActor.GroggyPending || nextActor.GroggyActive);
        if (skipEnemyTurn && nextOpponent != null)
        {
            bonusTurnGranted = true;
            bonusActor = nextOpponent.ActorType;

            nextActor.GroggyPending = false;
            nextActor.GroggyActive = false;
            if (nextActor.ActorType == CombatActorType.Enemy)
            {
                nextActor.EnemyGuard = Mathf.Max(1, nextActor.MaxEnemyGuard);
            }
            runtimeState.TurnState = CombatTurnState.PlayerTurn;
        }

        if (endingActor.ActorType == CombatActorType.Enemy && nextOpponent != null && nextOpponent.ActorType == CombatActorType.Player)
        {
            nextOpponent.GuardValue = 0;
        }

        // Guard expires when the owning actor's next turn starts (including bonus turns).
        CombatActorRuntime startActor = runtimeState.GetActor(runtimeState.TurnState);
        if (startActor != null && startActor.ActorType == CombatActorType.Player)
        {
            startActor.GuardValue = 0;
        }

        runtimeState.TurnIndex += 1;
        runtimeState.SyncActorSand();

        if (runtimeState.Player.IsDead || runtimeState.Enemy.IsDead)
        {
            runtimeState.IsCombatEnded = true;
            runtimeState.TurnState = CombatTurnState.Ended;
        }

        return new TurnTransitionResult(bonusTurnGranted, bonusActor, forcedFallAmount, forcedFallActor);
    }
}
