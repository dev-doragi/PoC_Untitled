using UnityEngine;

/// <summary>
/// Debug-only runner that starts hourglass combat and forwards manual action requests to HourglassCombatManager.
/// </summary>
public class HourglassCombatDebugRunner : MonoBehaviour
{
    [SerializeField] private bool autoStartCombat = true;

    private void Start()
    {
        if (!autoStartCombat)
        {
            return;
        }

        HourglassCombatManager.Instance?.StartCombat();
    }

    [ContextMenu("StartCombat")]
    public void StartCombat()
    {
        HourglassCombatManager.Instance?.StartCombat();
    }

    [ContextMenu("RequestStrike")]
    public void RequestStrike()
    {
        HourglassCombatManager.Instance?.RequestStrike();
    }

    [ContextMenu("RequestPierce")]
    public void RequestPierce()
    {
        HourglassCombatManager.Instance?.RequestPierce();
    }

    [ContextMenu("RequestHex")]
    public void RequestHex()
    {
        HourglassCombatManager.Instance?.RequestHex();
    }

    [ContextMenu("RequestGuard")]
    public void RequestGuard()
    {
        HourglassCombatManager.Instance?.RequestGuard();
    }

    [ContextMenu("RequestEndTurn")]
    public void RequestEndTurn()
    {
        HourglassCombatManager.Instance?.RequestEndTurn();
    }

    // TODO(Test): Later replace manual debug runner with PlayMode tests.
}
