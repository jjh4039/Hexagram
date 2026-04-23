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

    [Header("Death Cinematic")]
    [Tooltip("화면을 하얗게 덮을 패널 (CanvasGroup 포함)")]
    [SerializeField] private CanvasGroup whiteScreenGroup;
    [SerializeField] private float slowMotionScale = 0.2f; // 얼마나 느려질 것인가? (0.2배속)
    [SerializeField] private float whiteOutDuration = 2.0f; // 화면이 하얗게 덮이는 데 걸리는 시간

    [Header("GameOver Cinematic")]
    [SerializeField] private SpriteRenderer worldBlackoutSprite; // 카메라 자식으로 들어갈 거대한 검은 화면
    [SerializeField] private GameObject gameOverUI; // 최종적으로 켜질 게임오버 캔버스
    [SerializeField] private float timeSlowDuration = 1.5f; // 시간이 완전히 멈추기까지 걸리는 시간
    [SerializeField] private float blackoutDuration = 2.0f; // 화면이 까맣게 덮이는 데 걸리는 시간

    public float SunsetDuration => sunsetDuration;

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
        Player player = GameManager.instance.player;
        player.canControl = false;
        player.rigid.linearVelocity = Vector2.zero;

        StartCoroutine(Co_FadeGameplayUI(false));
        CameraFollow.instance.SetTarget(bossTransform, cinematicCameraSpeed);
        yield return StartCoroutine(Co_AnimateLetterBox(true));

        onSunsetStart?.Invoke();
        yield return StartCoroutine(Co_SunsetEffect());
        onSunsetDone?.Invoke();

        yield return new WaitForSeconds(holdDuration + barAnimTime);

        CameraFollow.instance.ResetTargetToPlayer();

        // ★ [원상복구] 레터박스가 다 올라갈 때까지 기다리지 않고(StartCoroutine) 바로 조작권을 줍니다.
        StartCoroutine(Co_AnimateLetterBox(false));
        StartCoroutine(Co_FadeGameplayUI(true));

        player.canControl = true;

        onFinish?.Invoke();
    }

    private IEnumerator Co_FadeGameplayUI(bool isShowing)
    {
        float startAlpha = isShowing ? 0f : 1f;
        float endAlpha = isShowing ? 1f : 0f;
        float elapsed = 0f;

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

    public void PlayBossDeathCinematic(EnemyBoss boss)
    {
        StartCoroutine(Co_BossDeathCinematic(boss));
    }

    private IEnumerator Co_BossDeathCinematic(EnemyBoss boss)
    {
        Time.timeScale = slowMotionScale;

        if (CameraFollow.instance != null)
            CameraFollow.instance.HitShake(0.5f, 0.15f);

        if (whiteScreenGroup != null)
        {
            whiteScreenGroup.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < whiteOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                whiteScreenGroup.alpha = Mathf.Clamp01(elapsed / whiteOutDuration);
                yield return null;
            }
            whiteScreenGroup.alpha = 1f;
        }

        if (boss != null)
        {
            boss.TurnIntoStatue();
        }

        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = 1f;

        if (whiteScreenGroup != null)
        {
            float elapsed = 0f;
            float fadeInDuration = 1.5f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                whiteScreenGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                yield return null;
            }
            whiteScreenGroup.gameObject.SetActive(false);
        }

        Debug.Log("보스 처치 연출 완전 종료!");
    }

    public void PlayGameOverCinematic(Transform playerTransform)
    {
        StartCoroutine(Co_GameOverCinematic(playerTransform));
    }

    private IEnumerator Co_GameOverCinematic(Transform playerTransform)
    {
        // 1. 기존 전투 UI 숨기기 및 카메라 마우스 추적 해제
        StartCoroutine(Co_FadeGameplayUI(false));
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.isCinematicFocus = true;
            CameraFollow.instance.isCinematicZoom = true; // [수정됨] 줌 인 시작
        }

        // [추가됨] 플레이어를 화면 최상단으로 끌어올리기 위한 레이어 조작
        SpriteRenderer playerSR = playerTransform.GetComponentInChildren<SpriteRenderer>();
        if (playerSR != null && worldBlackoutSprite != null)
        {
            // 검은 화면을 매우 높은 숫자(예: 30000)로 설정하여 모든 맵과 적을 덮습니다.
            worldBlackoutSprite.sortingOrder = 30000;

            // 플레이어는 검은 화면보다 딱 1만큼 더 높게(30001) 설정하여 샌드위치 시킵니다.
            playerSR.sortingOrder = 30001;
        }

        float elapsed = 0f;
        float initialTimeScale = Time.timeScale;

        if (worldBlackoutSprite != null)
        {
            worldBlackoutSprite.gameObject.SetActive(true);
            Color startColor = worldBlackoutSprite.color;
            startColor.a = 0f;
            worldBlackoutSprite.color = startColor;
        }

        // 2. 시간 감속 및 화면 암전
        while (elapsed < blackoutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / blackoutDuration;

            if (elapsed < timeSlowDuration)
            {
                Time.timeScale = Mathf.Lerp(initialTimeScale, 0f, elapsed / timeSlowDuration);
            }
            else
            {
                Time.timeScale = 0f;
            }

            if (worldBlackoutSprite != null)
            {
                Color c = worldBlackoutSprite.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                worldBlackoutSprite.color = c;
            }

            yield return null;
        }

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(1.5f);

        // 4. 전용 게임 오버 UI 활성화
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
    }
}