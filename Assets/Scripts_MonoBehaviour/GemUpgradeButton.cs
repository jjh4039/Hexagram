using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Image))]
public class GemUpgradeButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action OnClicked;

    [Header("Color Settings")]
    [Tooltip("기본 색상 (이 색상을 기준으로 비활성화/호버 색상이 자동 결정됩니다)")]
    public Color baseColor = new Color(142f / 255f, 223f / 255f, 233f / 255f, 1f); // 기본 8EDFE9

    private Color _hoverColor = Color.white;
    private Color _disabledColor;

    private Image _image;
    private bool _isInteractable = true;
    private bool _isHovering = false;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.02f);
        UpdateVisual();
    }
    
    private void OnValidate()
    {
        _disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.02f);
        if (_image == null) _image = GetComponent<Image>();
        UpdateVisual();
    }

    public void SetInteractable(bool state)
    {
        _isInteractable = state;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isInteractable && eventData.button == PointerEventData.InputButton.Left)
        {
            OnClicked?.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_image == null) return;

        if (!_isInteractable)
            _image.color = _disabledColor;
        else if (_isHovering)
            _image.color = _hoverColor;
        else
            _image.color = baseColor;
    }
}