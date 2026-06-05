using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField] private GraphicRaycaster raycaster; // 캔버스의 레이캐스터
    [SerializeField] private EventSystem eventSystem;    // 이벤트를 처리할 시스템

    private PointerEventData pointerData;                 // 마우스 위치 데이터 (재사용)
    private readonly List<RaycastResult> results = new(); // 레이캐스트 결과를 담을 리스트
    private readonly StringBuilder pathBuilder = new();   // 문자열 조합용 빌더 (메모리 누수 방지)

    private void Start()
    {
        if (eventSystem != null)
        {
            // 매번 생성하지 않고 시작할 때 한 번만 생성하여 재사용합니다.
            pointerData = new PointerEventData(eventSystem);
        }
    }

    private void Update()
    {
        // 빌드된 게임(.exe)에서는 이 아래 코드가 아예 컴파일되지 않아 메모리를 1%도 쓰지 않습니다.
#if UNITY_EDITOR
        if (raycaster == null || eventSystem == null || pointerData == null)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        pointerData.position = Mouse.current.position.ReadValue();

        results.Clear();
        raycaster.Raycast(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log("=== UI Raycast Results ===");
            for (int i = 0; i < results.Count; i++)
            {
                string fullPath = GetTransformPath(results[i].gameObject.transform);
                Debug.Log($"[{i}] {fullPath}");
            }
        }
#endif
    }

    private string GetTransformPath(Transform current)
    {
        // StringBuilder를 사용하여 메모리 쓰레기 발생을 원천 차단합니다.
        pathBuilder.Clear();
        pathBuilder.Append(current.name);

        while (current.parent != null)
        {
            current = current.parent;
            pathBuilder.Insert(0, " / ");
            pathBuilder.Insert(0, current.name);
        }

        return pathBuilder.ToString();
    }
}