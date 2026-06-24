using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputDeviceType
{
    Keyboard,
    Mouse
}

public class InputStateManager : MonoBehaviour
{
    public static InputStateManager Instance { get; private set; }
    public PlayerInput Actions => inputActions;

    private PlayerInput inputActions;
    private bool _isForceMouseMode = false; // 마우스 모드 강제 유지 플래그

    [Header("Current State")] [SerializeField]
    private GamePhase currentPhase = GamePhase.SafeZone;

    [SerializeField] private InputState currentInputState = InputState.Normal;

    public GamePhase CurrentPhase => currentPhase;
    public InputState CurrentInputState => currentInputState;
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.Mouse;

    public event Action<InputState> OnInputStateChanged;
    public event Action<GamePhase> OnGamePhaseChanged;
    public event Action<InputDeviceType> OnInputDeviceChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            inputActions = new PlayerInput();
            inputActions.Enable();

            if (inputActions != null)
            {
                inputActions.Normal.Disable();
                inputActions.Combat.Disable();
                inputActions.UI.Disable();

                if (currentInputState == InputState.Normal) inputActions.Normal.Enable();
                else if (currentInputState == InputState.Combat) inputActions.Combat.Enable();
                else if (currentInputState == InputState.UI) inputActions.UI.Enable();
            }

            InputSystem.onActionChange += OnInputActionChange;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this && inputActions != null)
        {
            inputActions.Disable();
            InputSystem.onActionChange -= OnInputActionChange;
        }
    }

    private void OnInputActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;
            InputDeviceType newDevice = CurrentDevice;

            if (device is Keyboard) newDevice = InputDeviceType.Keyboard;
            else if (device is Mouse) newDevice = InputDeviceType.Mouse;

            if (_isForceMouseMode) newDevice = InputDeviceType.Mouse; // 강제 상태면 무조건 마우스로 덮어쓰기

            if (newDevice != CurrentDevice)
            {
                CurrentDevice = newDevice;
                OnInputDeviceChanged?.Invoke(CurrentDevice);
            }
        }
    }

    public void ChangeInputState(InputState newState)
    {
        if (currentInputState == newState) return;
        currentInputState = newState;

        if (newState == InputState.UI)
        {
            inputActions.Normal.Disable();
            inputActions.Combat.Disable();
            inputActions.UI.Enable();
        }
        else
        {
            inputActions.UI.Disable();
            inputActions.Normal.Enable();
            inputActions.Combat.Enable();
        }

        OnInputStateChanged?.Invoke(newState);
    }

    public void ChangeGamePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;

        currentPhase = newPhase;
        OnGamePhaseChanged?.Invoke(newPhase);

        if (newPhase == GamePhase.SafeZone) ChangeInputState(InputState.Normal);
        else if (newPhase == GamePhase.InCombat) ChangeInputState(InputState.Combat);
    }

    public bool TryOpenUI()
    {
        if (currentPhase == GamePhase.InCombat) return false;

        ChangeInputState(InputState.UI);
        return true;
    }

    public void CloseUI()
    {
        if (currentPhase == GamePhase.InCombat) ChangeInputState(InputState.Combat);
        else ChangeInputState(InputState.Normal);
    }

    public void SetInputActive(bool isActive)
    {
        if (isActive)
        {
            if (currentInputState == InputState.UI)
            {
                inputActions.UI.Enable();
            }
            else
            {
                inputActions.Normal.Enable();
                inputActions.Combat.Enable();
            }
        }
        else
        {
            inputActions.Normal.Disable();
            inputActions.Combat.Disable();
            inputActions.UI.Disable();
        }
    }

    public void SetForceMouseMode(bool isForced)
    {
        _isForceMouseMode = isForced;

        if (isForced && CurrentDevice != InputDeviceType.Mouse)
        {
            CurrentDevice = InputDeviceType.Mouse;
            OnInputDeviceChanged?.Invoke(CurrentDevice);
        }
    }
}