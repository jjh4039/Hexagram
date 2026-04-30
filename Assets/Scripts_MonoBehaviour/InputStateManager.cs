using System;
using UnityEngine;

// 게임 상태와 입력을 총괄하는 매니저
public class InputStateManager : MonoBehaviour
{
    public static InputStateManager Instance { get; private set; }
    public PlayerInput Actions => inputActions;

    private PlayerInput inputActions;

    [Header("Current State")]
    [SerializeField] private GamePhase currentPhase = GamePhase.SafeZone;
    [SerializeField] private InputState currentInputState = InputState.Normal;

    public GamePhase CurrentPhase => currentPhase;
    public InputState CurrentInputState => currentInputState;

    public event Action<InputState> OnInputStateChanged;
    public event Action<GamePhase> OnGamePhaseChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            inputActions = new PlayerInput();
            inputActions.Enable();

            ChangeInputState(InputState.Normal);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null) inputActions.Disable();
    }

    public void ChangeInputState(InputState newState)
    {
        if (currentInputState == newState) return;

        inputActions.Normal.Disable();
        inputActions.Combat.Disable();
        inputActions.UI.Disable();

        currentInputState = newState;

        if (newState == InputState.Normal) inputActions.Normal.Enable();
        else if (newState == InputState.Combat) inputActions.Combat.Enable();
        else if (newState == InputState.UI) inputActions.UI.Enable();

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

    // 추가: 현재 상태(State)는 유지한 채 입력만 강제로 켜거나 끄는 함수
    public void SetInputActive(bool isActive)
    {
        if (isActive)
        {
            if (currentInputState == InputState.Normal) inputActions.Normal.Enable();
            else if (currentInputState == InputState.Combat) inputActions.Combat.Enable();
            else if (currentInputState == InputState.UI) inputActions.UI.Enable();
        }
        else
        {
            inputActions.Normal.Disable();
            inputActions.Combat.Disable();
            inputActions.UI.Disable();
        }
    }
}