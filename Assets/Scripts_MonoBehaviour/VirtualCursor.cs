using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CursorType
{
    Default,
    Aim,
}

public class VirtualCursor : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Image _cursorImage;

    [Header("Cursor Settings")] [Tooltip("0: Default, 1: Aim")] [SerializeField]
    private Sprite[] cursorSprites;

    public CursorType CurrentCursorType { get; private set; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _cursorImage = GetComponent<Image>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        if (_cursorImage != null) _cursorImage.enabled = true;

        ChangeCursor(CursorType.Default);
    }

    private void Update()
    {
        Vector2 mouseScreenPos = Vector2.zero;

        if (InputStateManager.Instance)
        {
            var state = InputStateManager.Instance.CurrentInputState;
            var actions = InputStateManager.Instance.Actions;

            if (state == InputState.Normal)
                mouseScreenPos = actions.Normal.Look.ReadValue<Vector2>();
            else if (state == InputState.Combat)
                mouseScreenPos = actions.Combat.Look.ReadValue<Vector2>();
            else if (Mouse.current != null)
            {
                mouseScreenPos = Mouse.current.position.ReadValue();
                ChangeCursor(default);
            }
        }
        else if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue();
        }

        if (_rectTransform)
        {
            _rectTransform.position = mouseScreenPos;
        }
        else
        {
            transform.position = mouseScreenPos;
        }
    }

    public void ChangeCursor(CursorType type)
    {
        if (!_cursorImage || cursorSprites.Length == 0) return;
        CurrentCursorType = type;

        switch (type)
        {
            case CursorType.Default:
                if (cursorSprites.Length > 0) _cursorImage.sprite = cursorSprites[0];
                _rectTransform.pivot = new Vector2(0f, 1f);
                break;

            case CursorType.Aim:
                if (cursorSprites.Length > 1) _cursorImage.sprite = cursorSprites[1];
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;

            default:
                Debug.Log("정의되지 않은 커서 타입입니다.");
                break;
        }
    }
}