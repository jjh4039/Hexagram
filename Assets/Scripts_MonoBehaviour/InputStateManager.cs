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

    // 시간 딜레이(쿨타임) 변수 삭제, 순수 이전 방향만 기억함
    private Vector2 lastUIRawInput; 

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

    // 딜레이 0초! 순수하게 '새로 눌린' 방향만 즉각 추출
    public bool TryGetCleanUIInput(Vector2 rawInput, out Vector2 cleanInput)
    {
        cleanInput = Vector2.zero;

        // 1. 키보드에서 손을 완전히 뗐을 때 (상태 초기화)
        if (rawInput.sqrMagnitude < 0.1f)
        {
            lastUIRawInput = Vector2.zero;
            return false;
        }

        // 2. 시간 딜레이 없이, '기존에 0이었다가 방금 막 눌린 축'만 찾아냄
        bool xJustPressed = Mathf.Abs(rawInput.x) > 0.5f && Mathf.Abs(lastUIRawInput.x) < 0.1f;
        bool yJustPressed = Mathf.Abs(rawInput.y) > 0.5f && Mathf.Abs(lastUIRawInput.y) < 0.1f;

        // 3. 상태는 무조건 갱신 (손을 뗄 때 대각선 찌꺼기 방지용)
        lastUIRawInput = rawInput;

        // 4. 새로 눌린 방향을 즉시 뱉어냄
        if (xJustPressed)
        {
            cleanInput = new Vector2(Mathf.Sign(rawInput.x), 0);
            return true; // 즉각 실행
        }
        else if (yJustPressed)
        {
            cleanInput = new Vector2(0, Mathf.Sign(rawInput.y));
            return true; // 즉각 실행
        }

        // 키를 꾹 누르고 있는 도중에 값이 애매하게 변한(따닥) 상황이면 쿨하게 무시
        return false;
    }
}