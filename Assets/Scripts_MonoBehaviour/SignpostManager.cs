using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SignpostManager : MonoBehaviour
{
    [System.Serializable]
    public class SignpostData
    {
        [Tooltip("맵에 배치된 표지판의 Collider2D (IsTrigger 체크 필요)")]
        public Collider2D signpostCollider;

        [Tooltip("표지판 자식으로 있는 World Space Canvas의 UI (CanvasGroup 필요)")]
        public CanvasGroup guideUI;

        [HideInInspector] public Vector3 originalLocalPos;
        [HideInInspector] public Coroutine currentAnim;
        [HideInInspector] public bool isShowing;
        [HideInInspector] public bool isPlayerInside;

        // 추가: 표지판의 SpriteRenderer를 찾아서 저장할 변수
        [HideInInspector] public SpriteRenderer spriteRenderer;
    }

    [Header("Signposts Array")]
    public SignpostData[] signposts;

    // ★ [추가됨] 표지판의 외곽선(또는 밝기) 변경용 머터리얼
    [Header("Material Settings")]
    [Tooltip("0: 기본 머터리얼(멀 때), 1: 강조 머터리얼(가까울 때)")]
    public Material[] signpostMaterials;

    [Header("Animation Settings")]
    public float animDuration = 0.3f;
    public float slideOffset = 0.8f;

    [Header("Animation Curves (느낌 조절)")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Settings")]
    public float cameraUpOffset = 2.0f;

    private int activeSignpostCount = 0;

    private void Start()
    {
        for (int i = 0; i < signposts.Length; i++)
        {
            if (signposts[i].guideUI != null)
            {
                signposts[i].originalLocalPos = signposts[i].guideUI.transform.localPosition;
                signposts[i].guideUI.gameObject.SetActive(false);
                signposts[i].guideUI.alpha = 0f;
            }

            if (signposts[i].signpostCollider != null)
            {
                // 표지판 객체에서 SpriteRenderer를 자동으로 찾아둡니다.
                signposts[i].spriteRenderer = signposts[i].signpostCollider.GetComponentInChildren<SpriteRenderer>();

                SignpostTrigger trigger = signposts[i].signpostCollider.gameObject.AddComponent<SignpostTrigger>();
                trigger.Initialize(this, i);
            }
        }
    }

    private void Update()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsCutsceneActive) return;

        for (int i = 0; i < signposts.Length; i++)
        {
            if (signposts[i].isPlayerInside && !signposts[i].isShowing)
            {
                OnSignpostEnter(i);
            }
        }
    }

    public void OnSignpostEnter(int index)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsCutsceneActive) return;

        if (index < 0 || index >= signposts.Length) return;

        SignpostData data = signposts[index];
        if (data.isShowing) return;

        data.isShowing = true;
        activeSignpostCount++;

        // ★ [추가됨] 가까이 갔을 때 머터리얼 변경 (배열 1번)
        if (data.spriteRenderer != null && signpostMaterials.Length > 1)
        {
            data.spriteRenderer.material = signpostMaterials[1];
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetUIOffset(new Vector3(0, cameraUpOffset, 0));
        }

        if (data.currentAnim != null) StopCoroutine(data.currentAnim);
        data.currentAnim = StartCoroutine(Co_AnimateUI(data, true));
    }

    public void OnSignpostExit(int index)
    {
        if (index < 0 || index >= signposts.Length) return;

        SignpostData data = signposts[index];
        if (!data.isShowing) return;

        data.isShowing = false;
        activeSignpostCount--;
        if (activeSignpostCount < 0) activeSignpostCount = 0;

        // ★ [추가됨] 멀어졌을 때 기본 머터리얼로 복구 (배열 0번)
        if (data.spriteRenderer != null && signpostMaterials.Length > 0)
        {
            data.spriteRenderer.material = signpostMaterials[0];
        }

        if (activeSignpostCount == 0 && CameraFollow.instance != null)
        {
            CameraFollow.instance.ResetUIOffset();
        }

        if (data.currentAnim != null) StopCoroutine(data.currentAnim);
        data.currentAnim = StartCoroutine(Co_AnimateUI(data, false));
    }

    private IEnumerator Co_AnimateUI(SignpostData data, bool isShowing)
    {
        if (data.guideUI == null) yield break;

        Transform uiTransform = data.guideUI.transform;
        if (isShowing) data.guideUI.gameObject.SetActive(true);

        float timer = 0f;
        float startAlpha = data.guideUI.alpha;
        float targetAlpha = isShowing ? 1f : 0f;

        Vector3 targetPos = isShowing ? data.originalLocalPos : data.originalLocalPos - new Vector3(0, slideOffset, 0);
        Vector3 startPos = uiTransform.localPosition;

        if (isShowing && startAlpha <= 0.01f)
        {
            startPos = data.originalLocalPos - new Vector3(0, slideOffset, 0);
            uiTransform.localPosition = startPos;
        }

        while (timer < animDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animDuration;

            float fadeT = fadeCurve.Evaluate(t);
            float moveT = moveCurve.Evaluate(t);

            data.guideUI.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeT);
            uiTransform.localPosition = Vector3.Lerp(startPos, targetPos, moveT);

            yield return null;
        }

        data.guideUI.alpha = targetAlpha;
        uiTransform.localPosition = targetPos;

        if (!isShowing) data.guideUI.gameObject.SetActive(false);
    }
}

public class SignpostTrigger : MonoBehaviour
{
    private SignpostManager manager;
    private int index;

    public void Initialize(SignpostManager mgr, int idx)
    {
        manager = mgr;
        index = idx;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && manager != null)
        {
            manager.signposts[index].isPlayerInside = true;
            manager.OnSignpostEnter(index);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && manager != null)
        {
            manager.signposts[index].isPlayerInside = false;
            manager.OnSignpostExit(index);
        }
    }
}