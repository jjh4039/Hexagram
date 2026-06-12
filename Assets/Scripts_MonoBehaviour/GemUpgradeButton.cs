using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Image))]
public class GemUpgradeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action OnClicked;

    [Header("Color Settings")]
    public Color baseColor = new Color(142f / 255f, 223f / 255f, 233f / 255f, 1f); // 기본 색상

    [Header("Auto Click Settings")]
    public float autoClickInitialDelay = 0.4f;  // 연사 대기 시간
    public float autoClickStartRate = 0.15f;    // 연사 초기 간격
    public float autoClickMinRate = 0.06f;      // 연사 최대 속도
    public float autoClickAcceleration = 0.85f; // 연사 가속도

    private Color _hoverColor = Color.white;
    private Color _disabledColor;

    private Image _image;
    private bool _isInteractable = true;
    private bool _isHovering = false;
    
    private bool _isHolding = false;            // 꾹 누름 상태
    private float _holdTimer = 0f;              // 연사 타이머
    private float _currentRate = 0f;            // 적용중인 연사 간격
    private bool _isFirstDelay = false;         // 최초 대기 상태

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
        if (!_isInteractable) _isHolding = false; 
        
        UpdateVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isInteractable || eventData.button != PointerEventData.InputButton.Left) return;

        OnClicked?.Invoke(); 

        _isHolding = true;
        _isFirstDelay = true;
        _holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _isHolding = false; // 범위 이탈 시 끝
        UpdateVisual();
    }

    private void Update()
    {
        if (!_isHolding || !_isInteractable) return;

        _holdTimer += Time.unscaledDeltaTime; 

        if (_isFirstDelay)
        {
            if (_holdTimer >= autoClickInitialDelay)
            {
                _isFirstDelay = false;
                _holdTimer = 0f;
                _currentRate = autoClickStartRate;
                OnClicked?.Invoke();
            }
        }
        else
        {
            if (_holdTimer >= _currentRate)
            {
                _holdTimer = 0f;
                OnClicked?.Invoke();

                if (_currentRate > autoClickMinRate)
                {
                    _currentRate *= autoClickAcceleration;
                    if (_currentRate < autoClickMinRate) _currentRate = autoClickMinRate;
                }
            }
        }
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