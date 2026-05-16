using UnityEngine;

/// <summary>
/// Runtime state for a single combat actor.
/// </summary>
public class CombatActorRuntime
{
    public CombatActorType ActorType;
    public int MaxHp;
    public int CurrentHp;
    public int MaxActionSand = 3;
    public int AvailableSand;
    public int SpentSand;
    public int TransferredSand;
    public int GuardValue;
    public int EnemyPrepStack;
    public int EnemyGuard;
    public int MaxEnemyGuard;
    public bool GroggyPending;
    public bool GroggyActive;

    public bool IsDead => CurrentHp <= 0;

    public bool CanAct => !IsDead && AvailableSand > 0;

    public bool CanSpend(int cost)
    {
        return cost > 0 && AvailableSand >= cost && TransferredSand + cost <= MaxActionSand;
    }

    public bool SpendSand(int cost)
    {
        return SpendSand(cost, out _);
    }

    public bool SpendSand(int cost, out string failureReason)
    {
        failureReason = GetSpendFailureReason(cost);
        if (failureReason != null)
        {
            return false;
        }

        AvailableSand -= cost;
        SpentSand += cost;
        TransferredSand += cost;
        return true;
    }

    public bool SpendDesperationSand()
    {
        return SpendDesperationSand(out _);
    }

    public bool SpendDesperationSand(out string failureReason)
    {
        const int desperationCost = 1;
        return SpendSand(desperationCost, out failureReason);
    }

    public int ReceiveSand(int amount, bool applyGroggyReduction, float groggyIncomingSandMultiplier)
    {
        if (amount <= 0)
        {
            AvailableSand = 0;
            return 0;
        }

        int received = applyGroggyReduction
            ? Mathf.CeilToInt(amount * groggyIncomingSandMultiplier)
            : amount;

        if (received < 0)
        {
            received = 0;
        }

        AvailableSand = received;
        return received;
    }

    public void ConsumeTurnSand()
    {
        SpentSand = 0;
        TransferredSand = 0;
    }

    public string GetSpendFailureReason(int cost)
    {
        if (cost <= 0)
        {
            return "Cost must be greater than zero.";
        }

        if (AvailableSand < cost)
        {
            return "AvailableSand insufficient";
        }

        if (TransferredSand + cost > MaxActionSand)
        {
            return "TransferredSand + Cost > MaxActionSand";
        }

        return null;
    }

    public static CombatActorRuntime CreateFromData(CombatActorDataSO data, int initialSand, int maxActionSand, int initialEnemyGuard = 0)
    {
        if (data == null)
        {
            return null;
        }

        bool isEnemy = data.actorType == CombatActorType.Enemy;
        int safeInitialEnemyGuard = Mathf.Max(0, initialEnemyGuard);

        return new CombatActorRuntime
        {
            ActorType = data.actorType,
            MaxHp = data.maxHp,
            CurrentHp = data.maxHp,
            MaxActionSand = maxActionSand,
            AvailableSand = initialSand,
            SpentSand = 0,
            TransferredSand = 0,
            GuardValue = data.baseGuard,
            EnemyPrepStack = 0,
            EnemyGuard = isEnemy ? safeInitialEnemyGuard : 0,
            MaxEnemyGuard = isEnemy ? safeInitialEnemyGuard : 0,
            GroggyPending = false,
            GroggyActive = false
        };
    }
}
