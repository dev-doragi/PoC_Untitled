using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-78)]
[RequireComponent(typeof(PlayerInput))]
public class InputReader : Singleton<InputReader>
{
    [Header("Action Maps")]
    [SerializeField] private string _playerActionMapName = "Player";
    [SerializeField] private string _uiActionMapName = "UI";
    [SerializeField] private string _systemActionMapName = "System";

    [Header("Player Actions")]
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _lookActionName = "Look";
    [SerializeField] private string _primaryActionName = "PrimaryAttack";
    [SerializeField] private string _secondaryActionName = "SecondaryAttack";

    [Header("UI/System Actions")]
    [SerializeField] private string _submitActionName = "Submit";
    [SerializeField] private string _cancelActionName = "Cancel";
    [SerializeField] private string _pauseActionName = "Pause";

    private PlayerInput _playerInput;
    private InputActionMap _playerMap;
    private InputActionMap _uiMap;
    private InputActionMap _systemMap;

    private InputAction _move;
    private InputAction _look;
    private InputAction _primary;
    private InputAction _secondary;
    private InputAction _submit;
    private InputAction _cancel;
    private InputAction _pause;

    protected override void OnBootstrap()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null || _playerInput.actions == null)
        {
            Debug.LogError("[InputReader] PlayerInput or InputActionAsset is missing.", this);
            return;
        }

        _playerMap = _playerInput.actions.FindActionMap(_playerActionMapName, false);
        _uiMap = _playerInput.actions.FindActionMap(_uiActionMapName, false);
        _systemMap = _playerInput.actions.FindActionMap(_systemActionMapName, false);

        if (_playerMap == null || _uiMap == null || _systemMap == null)
        {
            Debug.LogError("[InputReader] Required action map is missing. Expected: Player/UI/System", this);
            return;
        }

        _move = RequireAction(_playerMap, _moveActionName);
        _look = RequireAction(_playerMap, _lookActionName);
        _primary = RequireAction(_playerMap, _primaryActionName);
        _secondary = RequireAction(_playerMap, _secondaryActionName);
        _submit = RequireAction(_uiMap, _submitActionName);
        _cancel = RequireAction(_uiMap, _cancelActionName);
        _pause = RequireAction(_systemMap, _pauseActionName);
        if (_move == null || _look == null || _primary == null || _secondary == null || _submit == null || _cancel == null || _pause == null)
        {
            return;
        }

        Bind();
        _playerMap.Enable();
        _uiMap.Enable();
        _systemMap.Enable();

        _playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        Unbind();

        if (_playerInput != null)
        {
            _playerInput.onControlsChanged -= OnControlsChanged;
        }

        _playerMap?.Disable();
        _uiMap?.Disable();
        _systemMap?.Disable();
    }

    private InputAction RequireAction(InputActionMap map, string actionName)
    {
        InputAction action = map.FindAction(actionName, false);
        if (action == null)
        {
            Debug.LogError($"[InputReader] Missing required action '{actionName}' in map '{map.name}'.", this);
        }

        return action;
    }

    private void Bind()
    {
        _move.performed += OnMovePerformed;
        _move.canceled += OnMoveCanceled;
        _look.performed += OnLookPerformed;
        _look.canceled += OnLookCanceled;

        _primary.started += OnPrimaryStarted;
        _primary.canceled += OnPrimaryCanceled;
        _secondary.started += OnSecondaryStarted;
        _secondary.canceled += OnSecondaryCanceled;

        _submit.performed += OnSubmit;
        _cancel.performed += OnCancel;
        _pause.performed += OnPause;
    }

    private void Unbind()
    {
        if (_move != null)
        {
            _move.performed -= OnMovePerformed;
            _move.canceled -= OnMoveCanceled;
        }

        if (_look != null)
        {
            _look.performed -= OnLookPerformed;
            _look.canceled -= OnLookCanceled;
        }

        if (_primary != null)
        {
            _primary.started -= OnPrimaryStarted;
            _primary.canceled -= OnPrimaryCanceled;
        }

        if (_secondary != null)
        {
            _secondary.started -= OnSecondaryStarted;
            _secondary.canceled -= OnSecondaryCanceled;
        }

        if (_submit != null) _submit.performed -= OnSubmit;
        if (_cancel != null) _cancel.performed -= OnCancel;
        if (_pause != null) _pause.performed -= OnPause;
    }

    private static void OnControlsChanged(PlayerInput input)
    {
        string deviceName = input.currentControlScheme;
        EventBus.Instance.Publish(new InputDeviceChangedEvent { DeviceName = deviceName });
    }

    private static void OnMovePerformed(InputAction.CallbackContext ctx) => EventBus.Instance.Publish(new MoveInputEvent { Value = ctx.ReadValue<Vector2>() });
    private static void OnMoveCanceled(InputAction.CallbackContext ctx) => EventBus.Instance.Publish(new MoveInputEvent { Value = Vector2.zero });
    private static void OnLookPerformed(InputAction.CallbackContext ctx) => EventBus.Instance.Publish(new LookInputEvent { Value = ctx.ReadValue<Vector2>() });
    private static void OnLookCanceled(InputAction.CallbackContext ctx) => EventBus.Instance.Publish(new LookInputEvent { Value = Vector2.zero });
    private static void OnPrimaryStarted(InputAction.CallbackContext _) => EventBus.Instance.Publish(new PrimaryActionInputEvent { IsPressed = true });
    private static void OnPrimaryCanceled(InputAction.CallbackContext _) => EventBus.Instance.Publish(new PrimaryActionInputEvent { IsPressed = false });
    private static void OnSecondaryStarted(InputAction.CallbackContext _) => EventBus.Instance.Publish(new SecondaryActionInputEvent { IsPressed = true });
    private static void OnSecondaryCanceled(InputAction.CallbackContext _) => EventBus.Instance.Publish(new SecondaryActionInputEvent { IsPressed = false });
    private static void OnSubmit(InputAction.CallbackContext _) => EventBus.Instance.Publish(new SubmitInputEvent());
    private static void OnCancel(InputAction.CallbackContext _) => EventBus.Instance.Publish(new CancelInputEvent());
    private static void OnPause(InputAction.CallbackContext _) => EventBus.Instance.Publish(new PauseInputEvent());
}
