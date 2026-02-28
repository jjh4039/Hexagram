using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager instance;

    [Header("Cinematic Bars UI")]
    [SerializeField] private RectTransform topBar;      // 상단 검은 줄
    [SerializeField] private RectTransform bottomBar;   // 하단 검은 줄
    [SerializeField] private float barHeight = 40f;    // 최종 검은 줄의 두께
    [SerializeField] private float barAnimTime = 1.5f;  // 스르륵 나오는 시간

    [Header("Environment Sunset")]
    [SerializeField] private Color nightColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 저녁 색상
    [SerializeField] private float sunsetDuration = 2f; // 해가 지는 시간
    [SerializeField] private float holdDuration = 2f;   // 해가 지고 난 뒤 머무는 시간
    [SerializeField] private float cinematicCameraSpeed = 1.5f; // 컷신 중 카메라 이동 속도

    public float SunsetDuration => sunsetDuration;

    // ★ [추가] 숨길 UI 목록
    [Header("UI to Hide During Cinematic")]
    [Tooltip("컷신 중 숨길 UI 오브젝트들을 넣으세요 (Dice, Player_Info, Weapon 등)")]
    [SerializeField] private GameObject[] uiElementsToHide;
    [SerializeField] private float uiFadeTime = 0.3f; // UI가 사라지고 나타나는 속도

    private List<CanvasGroup> hiddenUIGroups = new List<CanvasGroup>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // ★ 할당된 UI 오브젝트들에 CanvasGroup이 없으면 자동으로 추가해서 리스트에 저장합니다.
        foreach (var obj in uiElementsToHide)
        {
            if (obj != null)
            {
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg == null) cg = obj.AddComponent<CanvasGroup>();
                hiddenUIGroups.Add(cg);
            }
        }
    }

    public IEnumerator Co_PlayBossIntro(Transform bossTransform, System.Action onSunsetStart, System.Action onSunsetDone, System.Action onFinish)
    {
        // 1. 플레이어 조작 봉쇄
        Player player = GameManager.instance.player;
        player.enabled = false;
        player.rigid.linearVelocity = Vector2.zero;

        StartCoroutine(Co_FadeGameplayUI(false));

        // 2. 카메라 보스 고정
        CameraFollow.instance.SetTarget(bossTransform, cinematicCameraSpeed);

        // 3. 레터박스 스르륵 등장!
        yield return StartCoroutine(Co_AnimateLetterBox(true));

        // ==========================================
        // ★ 4. 지형 어두워짐 시작 (이때 보스에게 신호 보냄)
        onSunsetStart?.Invoke();

        yield return StartCoroutine(Co_SunsetEffect());

        // ★ 5. 지형 어두워짐 완료! (이때 보스가 기동하며 쾅! 신호 보냄)
        onSunsetDone?.Invoke();
        // ==========================================

        // 6. 분위기 잡기 대기
        // 이전의 묵직한 타이밍 유지를 위해 대기
        yield return new WaitForSeconds(holdDuration + barAnimTime);

        // ==========================================
        // ★ 7. 전투 시작 신호탄! (동시다발적 실행)
        // ==========================================

        // 7-1. 카메라는 플레이어에게 복귀 시작
        CameraFollow.instance.ResetTargetToPlayer();

        // 7-2. 레터박스 퇴장 시작 (yield를 빼서 기다리지 않음)
        StartCoroutine(Co_AnimateLetterBox(false));

        // 7-3. UI 켜기 시작
        StartCoroutine(Co_FadeGameplayUI(true));

        // 7-4. 플레이어 조작 즉시 복구!
        player.enabled = true;

        // 7-5. 컷신 완전 종료 콜백 (보스 체력바가 이때부터 차오름)
        onFinish?.Invoke();
    }

    // ★ [추가] UI 투명도 조절 코루틴
    private IEnumerator Co_FadeGameplayUI(bool isShowing)
    {
        float startAlpha = isShowing ? 0f : 1f;
        float endAlpha = isShowing ? 1f : 0f;
        float elapsed = 0f;

        // UI를 숨길 때는 터치(클릭)도 안 되게 막아줍니다.
        foreach (var cg in hiddenUIGroups)
        {
            cg.interactable = isShowing;
            cg.blocksRaycasts = isShowing;
        }

        while (elapsed < uiFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeTime;

            foreach (var cg in hiddenUIGroups)
            {
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }
            yield return null;
        }

        foreach (var cg in hiddenUIGroups)
        {
            cg.alpha = endAlpha;
        }
    }

    private IEnumerator Co_AnimateLetterBox(bool isShowing)
    {
        if (topBar == null || bottomBar == null) yield break;

        topBar.gameObject.SetActive(true);
        bottomBar.gameObject.SetActive(true);

        float startHeight = isShowing ? 0f : barHeight;
        float endHeight = isShowing ? barHeight : 0f;

        float elapsed = 0f;
        while (elapsed < barAnimTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / barAnimTime);

            float currentHeight = Mathf.Lerp(startHeight, endHeight, t);

            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, currentHeight);
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, currentHeight);

            yield return null;
        }

        topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, endHeight);
        bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, endHeight);

        if (!isShowing)
        {
            topBar.gameObject.SetActive(false);
            bottomBar.gameObject.SetActive(false);
        }
    }

    private IEnumerator Co_SunsetEffect()
    {
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj == null) yield break;

        Tilemap[] tilemaps = gridObj.GetComponentsInChildren<Tilemap>();

        float elapsed = 0f;
        while (elapsed < sunsetDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sunsetDuration;

            Color lerpedColor = Color.Lerp(Color.white, nightColor, t);

            foreach (Tilemap tm in tilemaps)
            {
                tm.color = lerpedColor;
            }
            yield return null;
        }

        foreach (Tilemap tm in tilemaps)
        {
            tm.color = nightColor;
        }
    }
}