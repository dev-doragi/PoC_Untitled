using UnityEngine;

/// <summary>
/// Debug-only runner that enters Playing state automatically for PoC combat scenes.
/// </summary>
public class GameFlowDebugRunner : MonoBehaviour
{
    [SerializeField] private bool _autoEnterPlaying = true;

    private void Start()
    {
        if (!_autoEnterPlaying)
        {
            return;
        }

        EnterPlaying();
    }

    [ContextMenu("Enter Playing")]
    public void EnterPlaying()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowDebugRunner] GameManager instance is missing.", this);
            return;
        }

        GameManager.Instance.ChangeState(GameState.Playing);
    }
}