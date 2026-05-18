using UnityEngine;

/// <summary>
/// Main menu panel button handlers for single-scene flow.
/// </summary>
public class MainMenuPanelPresenter : MonoBehaviour
{
    [Header("Quit")]
    [SerializeField] private bool _quitToDesktop = true;
    [Header("Guide")]
    [SerializeField] private GameObject _guidePanel;
    [SerializeField] private float _guideDismissDelay = 0.8f;

    private bool _isGuideWaitingForInput;
    private float _guideInputUnlockTime;

    private void Awake()
    {
        if (_guidePanel != null)
        {
            _guidePanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<SubmitInputEvent>(OnGuideDismissInput);
        EventBus.Instance.Subscribe<CancelInputEvent>(OnGuideDismissInput);
        EventBus.Instance.Subscribe<PrimaryActionInputEvent>(OnPrimaryActionInput);
        EventBus.Instance.Subscribe<SecondaryActionInputEvent>(OnSecondaryActionInput);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<SubmitInputEvent>(OnGuideDismissInput);
        EventBus.Instance.Unsubscribe<CancelInputEvent>(OnGuideDismissInput);
        EventBus.Instance.Unsubscribe<PrimaryActionInputEvent>(OnPrimaryActionInput);
        EventBus.Instance.Unsubscribe<SecondaryActionInputEvent>(OnSecondaryActionInput);
    }

    public void StartGame()
    {
        if (_guidePanel == null)
        {
            StartGameInternal();
            return;
        }

        _guidePanel.SetActive(true);
        _isGuideWaitingForInput = true;
        _guideInputUnlockTime = Time.unscaledTime + Mathf.Max(0f, _guideDismissDelay);
    }

    private void StartGameInternal()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenuPanelPresenter] GameManager instance is missing.", this);
            return;
        }

        GameManager.Instance.ChangeState(GameState.Playing);
    }

    public void ExitGame()
    {
        if (_quitToDesktop)
        {
            Application.Quit();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }

    private void OnGuideDismissInput(SubmitInputEvent evt)
    {
        TryDismissGuideAndStart();
    }

    private void OnGuideDismissInput(CancelInputEvent evt)
    {
        TryDismissGuideAndStart();
    }

    private void OnPrimaryActionInput(PrimaryActionInputEvent evt)
    {
        if (!evt.IsPressed)
        {
            return;
        }

        TryDismissGuideAndStart();
    }

    private void OnSecondaryActionInput(SecondaryActionInputEvent evt)
    {
        if (!evt.IsPressed)
        {
            return;
        }

        TryDismissGuideAndStart();
    }

    private void TryDismissGuideAndStart()
    {
        if (!_isGuideWaitingForInput)
        {
            return;
        }

        if (Time.unscaledTime < _guideInputUnlockTime)
        {
            return;
        }

        _isGuideWaitingForInput = false;
        if (_guidePanel != null)
        {
            _guidePanel.SetActive(false);
        }

        StartGameInternal();
    }
}
