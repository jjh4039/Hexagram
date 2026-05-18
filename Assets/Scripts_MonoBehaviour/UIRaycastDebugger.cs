using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField] private GraphicRaycaster raycaster;  // 광선 추적을 수행할 캔버스의 레이캐스터
    [SerializeField] private EventSystem eventSystem;     // 이벤트를 처리할 시스템

    private PointerEventData pointerData;                 // 마우스 위치 데이터를 담을 객체
    private readonly List<RaycastResult> results = new(); // 레이캐스트 결과를 담을 리스트

    private void Update()
    {
        if (raycaster == null || eventSystem == null)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        pointerData = new PointerEventData(eventSystem);
        pointerData.position = Mouse.current.position.ReadValue();

        results.Clear();
        raycaster.Raycast(pointerData, results);

        Debug.Log("=== UI Raycast Results ===");
        for (int i = 0; i < results.Count; i++)
        {
            string fullPath = GetTransformPath(results[i].gameObject.transform);
            Debug.Log($"[{i}] {fullPath}");
        }
    }

    private string GetTransformPath(Transform current)
    {
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + " / " + path;
        }

        return path;
    }
}