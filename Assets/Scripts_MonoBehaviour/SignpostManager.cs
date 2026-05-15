using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SignpostManager : MonoBehaviour
{
    public enum InteractType { Signpost, Debris }

    [System.Serializable]
    public class SignpostData
    {
        public Collider2D signpostCollider; // 충돌체
        public CanvasGroup guideUI; // UI 그룹
        public TextMeshProUGUI[] tmpTexts; // 인스펙터 할당 텍스트 배열

        [HideInInspector] public string[] originalTexts; // 원본 캐싱 배열
        [HideInInspector] public Vector3 originalLocalPos; // 초기 로컬 위치
        [HideInInspector] public Coroutine currentAnim; // 현재 애니메이션
        [HideInInspector] public bool isShowing; // 현재 표시 여부
        [HideInInspector] public bool isPlayerInside; // 플레이어 진입 여부
        [HideInInspector] public SpriteRenderer spriteRenderer; // 스프라이트 렌더러
    }

    [Header("Interactable Arrays")] 
    public SignpostData[] signposts; // 표지판 데이터 리스트
    public SignpostData[] debrisList; // 잔해 데이터 리스트

    [Header("Material Settings")] 
    public Material[] signpostMaterials; // 강조용 머터리얼 배열

    [Header("Animation Settings")] 
    public float animDuration = 0.3f; // 애니메이션 지속 시간
    public float slideOffset = 0.8f; // 슬라이드 이동 거리

    [Header("Animation Curves")] 
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 페이드 곡선
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 이동 곡선

    [Header("Camera Settings")] 
    public float cameraUpOffset = 2.0f; // 표지판 카메라 상승 값
    public float cameraDownOffset = 2.0f; // 잔해 카메라 하강 값

    private int activeInteractCount = 0; // 활성화된 상호작용 개수
    private float postCutsceneDelay = 0.25f; // 컷신 종료 후 안정화 대기 시간
    private float lastCutsceneTime = -10f; // 컷신 마지막 활성화 시간 추적용

    private void Start()
    {
        InitializeArray(signposts, InteractType.Signpost);
        InitializeArray(debrisList, InteractType.Debris);
    }

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

            if (array[i].tmpTexts != null && array[i].tmpTexts.Length > 0)
            {
                array[i].originalTexts = new string[array[i].tmpTexts.Length];
                for (int j = 0; j < array[i].tmpTexts.Length; j++)
                {
                    if (array[i].tmpTexts[j] != null)
                    {
                        array[i].originalTexts[j] = array[i].tmpTexts[j].text; // 원본 텍스트 캐싱
                    }
                }
            }

            if (array[i].signpostCollider != null)
            {
                array[i].spriteRenderer = array[i].signpostCollider.GetComponentInChildren<SpriteRenderer>();
                SignpostTrigger trigger = array[i].signpostCollider.gameObject.AddComponent<SignpostTrigger>();
                trigger.Initialize(this, i, type); // 트리거 초기화
            }
        }
    }

    private void Update()
    {
        if (TutorialManager.Instance != null)
        {
            if (TutorialManager.Instance.IsCutsceneActive)
            {
                lastCutsceneTime = Time.time;
                return;
            }

            // 컷신 종료 후 지정된 시간(0.5초) 동안은 상호작용 지연 (카메라 튀는 현상 방지)
            if (Time.time - lastCutsceneTime < postCutsceneDelay) return; 
        }

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
        if (TutorialManager.Instance != null)
        {
            // 컷신 진행 중이거나 안정화 대기 시간 중이면 실행 방지
            if (TutorialManager.Instance.IsCutsceneActive || Time.time - lastCutsceneTime < postCutsceneDelay) return;
        }

        SignpostData[] targetArray = (type == InteractType.Signpost) ? signposts : debrisList;
        if (index < 0 || index >= targetArray.Length) return;

        SignpostData data = targetArray[index];
        if (data.isShowing) return;

        data.isShowing = true;
        activeInteractCount++;

        if (data.spriteRenderer != null)
        {
            int matIndex = (type == InteractType.Signpost) ? 1 : 2;
            if (matIndex < signpostMaterials.Length)
            {
                data.spriteRenderer.material = signpostMaterials[matIndex]; // 강조 머터리얼 적용
            }
        }

        if (CameraFollow.instance != null)
        {
            float yOffset = (type == InteractType.Signpost) ? cameraUpOffset : -cameraDownOffset;
            CameraFollow.instance.SetUIOffset(new Vector3(0, yOffset, 0)); // 카메라 위치 조정
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

        if (data.spriteRenderer != null && signpostMaterials.Length > 0)
        {
            data.spriteRenderer.material = signpostMaterials[0]; // 기본 머터리얼 복구
        }

        if (activeInteractCount == 0 && CameraFollow.instance != null)
        {
            CameraFollow.instance.ResetUIOffset(); // 카메라 위치 복구
        }

        if (data.currentAnim != null) StopCoroutine(data.currentAnim);
        data.currentAnim = StartCoroutine(Co_AnimateUI(data, false, type));
    }

    private IEnumerator Co_AnimateUI(SignpostData data, bool isShowing, InteractType type)
    {
        if (data.guideUI == null) yield break;

        Transform uiTransform = data.guideUI.transform;

        if (isShowing)
        {
            data.guideUI.gameObject.SetActive(true);

            if (data.tmpTexts != null && data.originalTexts != null)
            {
                for (int i = 0; i < data.tmpTexts.Length; i++)
                {
                    if (data.tmpTexts[i] != null)
                    {
                        data.tmpTexts[i].text = string.Empty;
                    }
                }
            }

            yield return null;

            if (data.tmpTexts != null && data.originalTexts != null)
            {
                for (int i = 0; i < data.tmpTexts.Length; i++)
                {
                    if (data.tmpTexts[i] != null && i < data.originalTexts.Length)
                    {
                        data.tmpTexts[i].text = data.originalTexts[i];
                    }
                }
            }
        }

        float timer = 0f;
        float startAlpha = data.guideUI.alpha;
        float targetAlpha = isShowing ? 1f : 0f;

        Vector3 slideVec = new Vector3(0, slideOffset, 0);
        Vector3 hiddenPos = (type == InteractType.Signpost)
            ? data.originalLocalPos - slideVec
            : data.originalLocalPos + slideVec;

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

            data.guideUI.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeCurve.Evaluate(t));
            uiTransform.localPosition = Vector3.Lerp(startPos, targetPos, moveCurve.Evaluate(t));

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
    private SignpostManager.InteractType interactType;

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