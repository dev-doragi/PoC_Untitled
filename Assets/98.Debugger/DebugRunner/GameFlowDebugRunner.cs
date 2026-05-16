using UnityEngine;

/// <summary>
/// Debug-only runner that enters Playing state automatically for PoC combat scenes.
/// </summary>
public class GameFlowDebugRunner : MonoBehaviour
{
    [SerializeField] private bool _autoEnterPlaying = true;
    [SerializeField] private bool _onlyWhenBootingState = true;

    private void Start()
    {
        if (!_autoEnterPlaying)
        {
            return;
        }

        if (_onlyWhenBootingState && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Booting)
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
