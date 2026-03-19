using UnityEngine;
using UnityEngine.InputSystem; // ★ 최신 인풋 사용
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager instance;

    [Header("보유한 아티팩트 목록")] public List<ArtifactData> myArtifacts = new List<ArtifactData>();

    [Header("테스트용")] public ArtifactData testArtifact;

    // ★ 최신 인풋 시스템 변수
    private PlayerInput inputActions;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 1. 인풋 액션 생성
        inputActions = new PlayerInput();
    }

    private void OnEnable()
    {
        // 2. 활성화 및 이벤트 연결
        inputActions.Enable();
        // 아까 만든 Action 이름이 'TestInput'이라고 가정합니다.
        inputActions.Player.TestInput.performed += OnTestInput;
    }

    private void OnDisable()
    {
        // 3. 해제 (필수)
        inputActions.Player.TestInput.performed -= OnTestInput;
        inputActions.Disable();
    }

    // ★ G키가 눌렸을 때 실행될 함수 (Update문 필요 없음!)
    private void OnTestInput(InputAction.CallbackContext context)
    {
        if (testArtifact != null)
        {
            AddArtifact(testArtifact);
        }
    }

    public void AddArtifact(ArtifactData data)
    {
        // 1. 리스트에 추가
        myArtifacts.Add(data);

        // 2. (나중에) 스탯 적용 로직
        // PlayerStats.Apply(data); 

        Debug.Log($"아티팩트 획득: {data.artifactName}");

        // 3. UI 갱신 요청
        if (DashboardUI.instance != null && DashboardUI.instance.isOpen)
        {
            DashboardUI.instance.RefreshArtifacts();
        }
    }
}