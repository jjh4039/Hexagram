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
    [SerializeField] private float barHeight = 150f;    // 최종 검은 줄의 두께
    [SerializeField] private float barAnimTime = 0.5f;  // 스르륵 나오는 시간

    [Header("Environment Sunset")]
    [SerializeField] private Color nightColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 저녁 색상
    [SerializeField] private float sunsetDuration = 2f; // 해가 지는 시간
    [SerializeField] private float holdDuration = 1f;   // 해가 지고 난 뒤 머무는 시간
    [SerializeField] private float cinematicCameraSpeed = 1.5f; // 컷신 중 카메라 이동 속도

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

    public IEnumerator Co_PlayBossIntro(Transform bossTransform, System.Action onFinish)
    {
        // 1. 플레이어 조작 봉쇄
        Player player = GameManager.instance.player;
        player.enabled = false;
        player.rigid.linearVelocity = Vector2.zero;

        // ★ [추가] 컷신 시작 시 UI 서서히 숨기기 (코루틴을 대기하지 않고 즉시 실행)
        StartCoroutine(Co_FadeGameplayUI(false));

        // 2. 카메라 보스 고정
        CameraFollow.instance.SetTarget(bossTransform, cinematicCameraSpeed);

        // 3. 레터박스 스르륵 등장!
        yield return StartCoroutine(Co_AnimateLetterBox(true));

        // 4. 지형 서서히 어두워짐
        yield return StartCoroutine(Co_SunsetEffect());

        // 5. 분위기 잡기 대기
        yield return new WaitForSeconds(holdDuration);

        // 6. 레터박스 스르륵 퇴장!
        yield return StartCoroutine(Co_AnimateLetterBox(false));

        // 7. 복구
        CameraFollow.instance.ResetTargetToPlayer();
        player.enabled = true;

        // ★ [추가] 컷신 종료 시 UI 서서히 다시 켜기
        StartCoroutine(Co_FadeGameplayUI(true));

        // 8. 콜백 실행 (이후 체력바가 차오르기 시작함)
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