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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this && inputActions != null) 
            inputActions.Disable();
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
}