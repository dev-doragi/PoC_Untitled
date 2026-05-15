/// <summary>
/// Result payload for a single combat action resolution.
/// </summary>
public readonly struct CombatActionResult
{
    public readonly CombatActionType ActionType;
    public readonly bool Succeeded;
    public readonly int SpentSand;
    public readonly int DamageDealt;
    public readonly bool BreakTriggered;
    public readonly bool GroggyTriggered;
    public readonly string FailureReason;

    public CombatActionResult(
        CombatActionType actionType,
        bool succeeded,
        int spentSand,
        int damageDealt,
        bool breakTriggered,
        bool groggyTriggered,
        string failureReason = null)
    {
        ActionType = actionType;
        Succeeded = succeeded;
        SpentSand = spentSand;
        DamageDealt = damageDealt;
        BreakTriggered = breakTriggered;
        GroggyTriggered = groggyTriggered;
        FailureReason = failureReason;
    }
}

/// <summary>
/// Resolves action effects between source and target actor runtimes.
/// </summary>
public class CombatActionResolver
{
    public CombatActionResult Resolve(CombatActorRuntime source, CombatActorRuntime target, CombatActionDataSO actionData, int breakThreshold, int prepCap)
    {
        CombatActionType actionType = actionData != null ? actionData.actionType : CombatActionType.None;
        if (source == null || target == null || source.IsDead || target.IsDead)
        {
            return new CombatActionResult(actionType, false, 0, 0, false, false, "Invalid actor runtime or dead actor.");
        }

        if (actionData == null)
        {
            return new CombatActionResult(actionType, false, 0, 0, false, false, "ActionData missing");
        }

        if (actionType == CombatActionType.DesperationStrike)
        {
            return ResolveDesperationStrike(source, target, actionData, breakThreshold);
        }

        int cost = actionData.sandCost;
        if (!source.SpendSand(cost, out string spendFailureReason))
        {
            return new CombatActionResult(actionType, false, 0, 0, false, false, spendFailureReason);
        }

        if (actionType == CombatActionType.Strike)
        {
            int damage = ApplyDamage(target, actionData.baseDamage);
            return new CombatActionResult(actionType, true, cost, damage, false, false);
        }

        if (actionType == CombatActionType.Pierce)
        {
            int damage = ApplyDamage(target, actionData.baseDamage);
            bool breakTriggered = ApplyBreak(target, actionData.breakPower, breakThreshold);
            bool groggyTriggered = ApplyGroggy(target, breakTriggered);
            return new CombatActionResult(actionType, true, cost, damage, breakTriggered, groggyTriggered);
        }

        if (actionType == CombatActionType.Hex)
        {
            int damage = ApplyDamage(target, actionData.baseDamage);
            bool breakTriggered = ApplyBreak(target, actionData.breakPower, breakThreshold);
            bool groggyTriggered = ApplyGroggy(target, breakTriggered);
            target.EnemyPrepStack = target.EnemyPrepStack > 0 ? target.EnemyPrepStack - 1 : 0;
            return new CombatActionResult(actionType, true, cost, damage, breakTriggered, groggyTriggered);
        }

        if (actionType == CombatActionType.Guard)
        {
            source.GuardValue += actionData.guardValue;
            return new CombatActionResult(actionType, true, cost, 0, false, false);
        }

        if (actionType == CombatActionType.Prepare)
        {
            source.EnemyPrepStack += actionData.prepGain;
            if (source.EnemyPrepStack > prepCap)
            {
                source.EnemyPrepStack = prepCap;
            }

            return new CombatActionResult(actionType, true, cost, 0, false, false);
        }

        if (actionType == CombatActionType.WeakAttack)
        {
            int damage = ApplyDamage(target, actionData.baseDamage + source.EnemyPrepStack);
            source.EnemyPrepStack = 0;
            return new CombatActionResult(actionType, true, cost, damage, false, false);
        }

        return new CombatActionResult(actionType, false, 0, 0, false, false, "Unsupported action type.");
    }

    private CombatActionResult ResolveDesperationStrike(CombatActorRuntime source, CombatActorRuntime target, CombatActionDataSO actionData, int breakThreshold)
    {
        if (!source.SpendDesperationSand(out string spendFailureReason))
        {
            return new CombatActionResult(CombatActionType.DesperationStrike, false, 0, 0, false, false, spendFailureReason);
        }

        const int spent = 1;
        int damage = ApplyDamage(target, actionData.baseDamage + source.EnemyPrepStack);
        source.EnemyPrepStack = 0;
        bool breakTriggered = ApplyBreak(target, actionData.breakPower, breakThreshold);
        bool groggyTriggered = ApplyGroggy(target, breakTriggered);
        return new CombatActionResult(CombatActionType.DesperationStrike, true, spent, damage, breakTriggered, groggyTriggered);
    }

    private static int ApplyDamage(CombatActorRuntime target, int rawDamage)
    {
        int damage = rawDamage;
        if (target.GuardValue > 0)
        {
            int blocked = target.GuardValue < damage ? target.GuardValue : damage;
            target.GuardValue -= blocked;
            damage -= blocked;
        }

        if (damage <= 0)
        {
            return 0;
        }

        target.CurrentHp -= damage;
        if (target.CurrentHp < 0)
        {
            target.CurrentHp = 0;
        }

        return damage;
    }

    private static bool ApplyBreak(CombatActorRuntime target, int breakDelta, int breakThreshold)
    {
        target.BreakProgress += breakDelta;
        if (target.BreakProgress < breakThreshold)
        {
            return false;
        }

        target.BreakProgress = 0;
        return true;
    }

    private static bool ApplyGroggy(CombatActorRuntime target, bool breakTriggered)
    {
        if (!breakTriggered)
        {
            return false;
        }

        target.GroggyPending = true;
        return true;
    }
}
