using System;
using UnityEngine;

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
            DontDestroyOnLoad(gameObject); // ★ 글로벌 매니저로서 씬 전환 시 생존 보장
            
            inputActions = new PlayerInput();
            inputActions.Enable();

            // ★ 인포서 해제: 최초 생성 시에만 켜고, 씬 전환 시에는 기존 상태를 유지하여 강제 조작 활성화를 방지
            if (inputActions != null)
            {
                inputActions.Normal.Disable();
                inputActions.Combat.Disable();
                inputActions.UI.Disable();
                
                if (currentInputState == InputState.Normal) inputActions.Normal.Enable();
                else if (currentInputState == InputState.Combat) inputActions.Combat.Enable();
                else if (currentInputState == InputState.UI) inputActions.UI.Enable();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 글로벌 인스턴스가 파괴될 때(게임 종료 등)만 디스에이블 처리
        if (Instance == this && inputActions != null) 
            inputActions.Disable();
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