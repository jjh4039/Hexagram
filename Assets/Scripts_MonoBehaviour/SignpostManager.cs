using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SignpostManager : MonoBehaviour
{
    // 표지판인지 잔해인지 구분하기 위한 열거형
    public enum InteractType { Signpost, Debris }

    [System.Serializable]
    public class SignpostData
    {
        [Tooltip("맵에 배치된 오브젝트의 Collider2D (IsTrigger 체크 필요)")]
        public Collider2D signpostCollider;

        [Tooltip("자식으로 있는 World Space Canvas의 UI (CanvasGroup 필요)")]
        public CanvasGroup guideUI;

        [HideInInspector] public Vector3 originalLocalPos;
        [HideInInspector] public Coroutine currentAnim;
        [HideInInspector] public bool isShowing;
        [HideInInspector] public bool isPlayerInside;

        [HideInInspector] public SpriteRenderer spriteRenderer;
    }

    [Header("Interactable Arrays")]
    [Tooltip("기존 표지판 배열 (위로 카메라 이동, UI 아래에서 위로 등장)")]
    public SignpostData[] signposts;

    [Tooltip("새로운 잔해 배열 (아래로 카메라 이동, UI 위에서 아래로 등장)")]
    public SignpostData[] debrisList; // ★ [추가됨] 잔해 데이터 배열

    [Header("Material Settings")]
    [Tooltip("0: 기본, 1: 표지판 강조(청록), 2: 잔해 강조(주황)")]
    public Material[] signpostMaterials;

    [Header("Animation Settings")]
    public float animDuration = 0.3f;
    public float slideOffset = 0.8f;

    [Header("Animation Curves (느낌 조절)")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Settings")]
    public float cameraUpOffset = 2.0f;     // 표지판용 카메라 상승 오프셋
    public float cameraDownOffset = 2.0f;   // ★ [추가됨] 잔해용 카메라 하강 오프셋

    private int activeInteractCount = 0;

    private void Start()
    {
        // 1. 표지판 배열 초기화
        InitializeArray(signposts, InteractType.Signpost);

        // 2. 잔해 배열 초기화
        InitializeArray(debrisList, InteractType.Debris);
    }

    // 배열 초기화 중복 코드를 줄이기 위한 헬퍼 함수
    private void InitializeArray(SignpostData[] array, InteractType type)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].guideUI != null)
            {
                array[i].originalLocalPos = array[i].guideUI.transform.localPosition;
                array[i].guideUI.gameObject.SetActive(false);
                array[i].guideUI.alpha = 0f;
            }

            if (array[i].signpostCollider != null)
            {
                array[i].spriteRenderer = array[i].signpostCollider.GetComponentInChildren<SpriteRenderer>();

                SignpostTrigger trigger = array[i].signpostCollider.gameObject.AddComponent<SignpostTrigger>();
                trigger.Initialize(this, i, type); // 타입도 함께 넘겨줍니다.
            }
        }
    }

    private void Update()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsCutsceneActive) return;

        CheckArrayUpdate(signposts, InteractType.Signpost);
        CheckArrayUpdate(debrisList, InteractType.Debris);
    }

    private void CheckArrayUpdate(SignpostData[] array, InteractType type)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].isPlayerInside && !array[i].isShowing)
            {
                OnInteractEnter(i, type);
            }
        }
    }

    public void OnInteractEnter(int index, InteractType type)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsCutsceneActive) return;

        SignpostData[] targetArray = (type == InteractType.Signpost) ? signposts : debrisList;
        if (index < 0 || index >= targetArray.Length) return;

        SignpostData data = targetArray[index];
        if (data.isShowing) return;

        data.isShowing = true;
        activeInteractCount++;

        // ★ 머터리얼 처리: 표지판은 1번, 잔해는 2번 사용
        if (data.spriteRenderer != null)
        {
            int matIndex = (type == InteractType.Signpost) ? 1 : 2;
            if (matIndex < signpostMaterials.Length)
            {
                data.spriteRenderer.material = signpostMaterials[matIndex];
            }
        }

        // ★ 카메라 오프셋: 표지판은 위로(+), 잔해는 아래로(-)
        if (CameraFollow.instance != null)
        {
            float yOffset = (type == InteractType.Signpost) ? cameraUpOffset : -cameraDownOffset;
            CameraFollow.instance.SetUIOffset(new Vector3(0, yOffset, 0));
        }

        if (data.currentAnim != null) StopCoroutine(data.currentAnim);
        data.currentAnim = StartCoroutine(Co_AnimateUI(data, true, type));
    }

    public void OnInteractExit(int index, InteractType type)
    {
        SignpostData[] targetArray = (type == InteractType.Signpost) ? signposts : debrisList;
        if (index < 0 || index >= targetArray.Length) return;

        SignpostData data = targetArray[index];
        if (!data.isShowing) return;

        data.isShowing = false;
        activeInteractCount--;
        if (activeInteractCount < 0) activeInteractCount = 0;

        // 멀어졌을 때 기본 머터리얼(0번)로 복구
        if (data.spriteRenderer != null && signpostMaterials.Length > 0)
        {
            data.spriteRenderer.material = signpostMaterials[0];
        }

        if (activeInteractCount == 0 && CameraFollow.instance != null)
        {
            CameraFollow.instance.ResetUIOffset();
        }

        if (data.currentAnim != null) StopCoroutine(data.currentAnim);
        data.currentAnim = StartCoroutine(Co_AnimateUI(data, false, type));
    }

    private IEnumerator Co_AnimateUI(SignpostData data, bool isShowing, InteractType type)
    {
        if (data.guideUI == null) yield break;

        Transform uiTransform = data.guideUI.transform;
        if (isShowing) data.guideUI.gameObject.SetActive(true);

        float timer = 0f;
        float startAlpha = data.guideUI.alpha;
        float targetAlpha = isShowing ? 1f : 0f;

        // ★ 타입에 따른 시작/끝 위치(Hidden Position) 분기 처리
        // 표지판: 원래 위치보다 아래쪽 (-slideOffset)에 숨어있음
        // 잔해: 원래 위치보다 위쪽 (+slideOffset)에 숨어있음
        Vector3 slideVec = new Vector3(0, slideOffset, 0);
        Vector3 hiddenPos = (type == InteractType.Signpost) ? data.originalLocalPos - slideVec : data.originalLocalPos + slideVec;

        Vector3 targetPos = isShowing ? data.originalLocalPos : hiddenPos;
        Vector3 startPos = uiTransform.localPosition;

        if (isShowing && startAlpha <= 0.01f)
        {
            startPos = hiddenPos;
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

// ---------------------------------------------------------
// 충돌 감지용 보조 스크립트 (타입 구별 추가)
// ---------------------------------------------------------
public class SignpostTrigger : MonoBehaviour
{
    private SignpostManager manager;
    private int index;
    private SignpostManager.InteractType interactType; // 타입 변수 추가

    public void Initialize(SignpostManager mgr, int idx, SignpostManager.InteractType type)
    {
        manager = mgr;
        index = idx;
        interactType = type;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && manager != null)
        {
            // 타입에 맞게 데이터 갱신
            if (interactType == SignpostManager.InteractType.Signpost)
                manager.signposts[index].isPlayerInside = true;
            else
                manager.debrisList[index].isPlayerInside = true;

            manager.OnInteractEnter(index, interactType);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && manager != null)
        {
            if (interactType == SignpostManager.InteractType.Signpost)
                manager.signposts[index].isPlayerInside = false;
            else
                manager.debrisList[index].isPlayerInside = false;

            manager.OnInteractExit(index, interactType);
        }
    }
}