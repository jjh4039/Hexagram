using System;
using UnityEngine;

// 게임 상태와 입력을 총괄하는 매니저
public class InputStateManager : MonoBehaviour
{
    public static InputStateManager Instance { get; private set; } // 전역 접근용 인스턴스
    public PlayerInput Actions => inputActions;                    // 외부 접근용 입력 프로퍼티

    private PlayerInput inputActions;                              // 자동 생성된 입력 시스템 클래스

    [Header("Current State")]
    [SerializeField] private GamePhase currentPhase = GamePhase.SafeZone;      // 인스펙터 확인용 상황
    [SerializeField] private InputState currentInputState = InputState.Normal; // 인스펙터 확인용 조작

    public GamePhase CurrentPhase => currentPhase;                             // 외부 읽기용 상황
    public InputState CurrentInputState => currentInputState;                  // 외부 읽기용 조작

    public event Action<InputState> OnInputStateChanged;           // 조작 상태 변경 알림
    public event Action<GamePhase> OnGamePhaseChanged;             // 게임 상황 변경 알림

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            inputActions = new PlayerInput();               // 입력 객체 생성
            inputActions.Enable();                          // 입력 시스템 가동
            
            ChangeInputState(InputState.Normal);            // 시작 시 기본 조작 설정
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null) inputActions.Disable();   // 파괴 시 메모리 누수 방지
    }

    // 기존 맵을 끄고 필요한 맵만 켭니다
    public void ChangeInputState(InputState newState)
    {
        if (currentInputState == newState) return;
        
        if (newState == InputState.UI)
        {
            StopPlayerMovement();
        }

        inputActions.Normal.Disable();                      // 평상시 조작 끄기
        inputActions.Combat.Disable();                      // 전투용 조작 끄기
        inputActions.UI.Disable();                          // 화면 조작 끄기

        currentInputState = newState;

        if (newState == InputState.Normal) inputActions.Normal.Enable();      // 평상시 조작 켜기
        else if (newState == InputState.Combat) inputActions.Combat.Enable(); // 전투용 조작 켜기
        else if (newState == InputState.UI) inputActions.UI.Enable();         // 화면 조작 켜기
        
        OnInputStateChanged?.Invoke(newState);
    }

    // 상황을 변경하고 기본 조작을 갱신합니다
    public void ChangeGamePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        currentPhase = newPhase;
        OnGamePhaseChanged?.Invoke(newPhase);

        if (newPhase == GamePhase.SafeZone) ChangeInputState(InputState.Normal);
        else if (newPhase == GamePhase.InCombat) ChangeInputState(InputState.Combat);
    }

    // 안전할 때만 화면 조작으로 전환합니다
    public bool TryOpenUI()
    {
        if (currentPhase == GamePhase.InCombat) return false;

        ChangeInputState(InputState.UI);
        return true;
    }

    // 조작을 끝내고 이전 상태로 복귀합니다
    public void CloseUI()
    {
        if (currentPhase == GamePhase.InCombat) ChangeInputState(InputState.Combat);
        else ChangeInputState(InputState.Normal);
    }
    
    private void StopPlayerMovement()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return;

        // 1. Rigidbody의 속도를 0으로 (미끄러짐 방지)
        var rb = GameManager.instance.player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

    }
}