using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    private PointerEventData pointerData;
    private readonly List<RaycastResult> results = new();

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
        foreach (var result in results)
        {
            Debug.Log(result.gameObject.name);
        }
    }
}