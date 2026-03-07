using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

public class Dice_UI : MonoBehaviour
{
    [Header("--- Sound Settings ---")]
    [SerializeField] private AudioClip rollSound;    // 주사위가 굴러가는 소리
    [SerializeField] private AudioClip resultSound;  // 결과가 팍 터질 때 소리

    [Header("--- UI References ---")]
    [SerializeField] private Image diceFillImage;
    [SerializeField] private Image gaugeFillImage;
    [SerializeField] private Image keyGuideImage;
    [SerializeField] private TextMeshProUGUI diceCountText;
    [SerializeField] private GameObject maxText;

    [Header("--- Sprites (0: Normal, 1: Max) ---")]
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Sprite[] gaugeSprites;
    [SerializeField] private Sprite[] keyGuideSprites;

    [Header("--- Text Colors ---")]
    [SerializeField] private Color textNormalColor = Color.white;
    [SerializeField] private Color textMaxColor = Color.yellow;

    [Header("--- Roll Animation Settings ---")]
    [SerializeField] private GameObject dice3DObject;
    [SerializeField] private GameObject diceObject;
    [SerializeField] private Image resultDiceImage;
    [SerializeField] private TextMeshProUGUI resultDiceText;

    [Space(10)]
    [SerializeField] private CanvasGroup fadeInGroup;
    [SerializeField] private CanvasGroup fadeOutGroup;

    [Header("--- Dice Visual Data ---")]
    [SerializeField] private Sprite[] resultDiceSprites;
    [SerializeField] private GlowFilter glowFilter;

    [Header("--- Burst Particle Settings ---")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private int burstCount = 6;

    private PlayerStats stats;
    private bool isRolling = false;
    private Vector3 originalCubeScale;
    private Vector3 originalDiceObjectScale;

    public bool IsRolling => isRolling;

    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.stats != null)
        {
            stats = GameManager.instance.stats;
        }

        if (dice3DObject != null)
        {
            originalCubeScale = dice3DObject.transform.localScale;
            dice3DObject.SetActive(false);
        }

        if (diceObject != null)
        {
            originalDiceObjectScale = diceObject.transform.localScale;
        }

        if (fadeInGroup != null) { fadeInGroup.alpha = 0f; fadeInGroup.gameObject.SetActive(false); }
        if (fadeOutGroup != null) { fadeOutGroup.alpha = 1f; fadeOutGroup.gameObject.SetActive(false); }
    }

    void Update()
    {
        if (stats == null) return;

        bool isMax = stats.currentDiceCharge >= stats.maxDiceCharge;

        UpdateDiceFill(isMax);
        UpdateKeyGuide(isMax);
        UpdateGaugeFill(isMax);
        UpdateText(isMax);
    }

    public void PlayRollAnimation(DiceData data, int diceIndex)
    {
        if (isRolling) return;
        StartCoroutine(SingleRollRoutine(data, diceIndex));
    }

    private IEnumerator SingleRollRoutine(DiceData data, int diceIndex)
    {
        isRolling = true;

        resultDiceText.text = data.shortDescription;
        if (glowFilter != null) glowFilter.Color = data.uiGlowColor;
        if (resultDiceSprites != null && diceIndex < resultDiceSprites.Length)
            resultDiceImage.sprite = resultDiceSprites[diceIndex];

        // 1. 주사위 UI 사라짐
        if (diceObject != null)
        {
            yield return StartCoroutine(ScaleObject(diceObject.transform, originalDiceObjectScale, originalDiceObjectScale * 1.2f, 10f));
            yield return StartCoroutine(ScaleObject(diceObject.transform, originalDiceObjectScale * 1.2f, Vector3.zero, 8f));
        }

        // 2. 3D 주사위 등장 및 회전 연출
        if (dice3DObject != null)
        {
            // [사운드] 주사위 굴러가는 소리 재생 (피치 변화를 주어 매번 다르게 들리게 함)
            if (SoundManager.instance != null)
                SoundManager.instance.PlaySFX(rollSound, 1.9f, 0.1f);

            dice3DObject.SetActive(true);
            Transform cubeTransform = dice3DObject.transform;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 8f;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                cubeTransform.localScale = Vector3.Lerp(Vector3.zero, originalCubeScale, easeT);
                yield return null;
            }
            cubeTransform.localScale = originalCubeScale;

            yield return new WaitForSeconds(0.4f);

            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 12f;
                float easeT = t * t;
                cubeTransform.localScale = Vector3.Lerp(originalCubeScale, Vector3.zero, easeT);
                yield return null;
            }
            dice3DObject.SetActive(false);
        }

        // 3. 결과창 페이드 인 및 확대
        fadeInGroup.gameObject.SetActive(true);
        fadeOutGroup.gameObject.SetActive(true);
        fadeInGroup.alpha = 0f;
        fadeOutGroup.alpha = 1f;

        Transform imgTransform = resultDiceImage.transform;
        Transform txtTransform = resultDiceText.transform;

        // [사운드] 결과가 팍! 터지는 임팩트 소리
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySFX(resultSound, 1.7f, 0.3f);

        SpawnBurstParticles(resultDiceImage.transform.position);

        float pt = 0;
        float fadeSpeed = 1.2f;
        while (pt < 1f)
        {
            pt += Time.deltaTime * 10f;
            float easeT = 1f - Mathf.Pow(1f - pt, 4f);
            imgTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, easeT);
            txtTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, easeT);
            fadeInGroup.alpha = Mathf.MoveTowards(fadeInGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        fadeInGroup.alpha = 1f;

        pt = 0;
        while (pt < 1f)
        {
            pt += Time.deltaTime * 15f;
            imgTransform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, pt);
            txtTransform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, pt);
            yield return null;
        }
        imgTransform.localScale = Vector3.one;
        txtTransform.localScale = Vector3.one;

        yield return new WaitForSeconds(1.5f);

        pt = 0;
        while (pt < 1f)
        {
            pt += Time.deltaTime * 3f;
            fadeOutGroup.alpha = Mathf.Lerp(1f, 0f, pt);
            yield return null;
        }
        fadeInGroup.gameObject.SetActive(false);
        fadeOutGroup.gameObject.SetActive(false);

        if (diceObject != null)
        {
            yield return StartCoroutine(ScaleObject(diceObject.transform, Vector3.zero, originalDiceObjectScale * 1.2f, 10f));
            yield return StartCoroutine(ScaleObject(diceObject.transform, originalDiceObjectScale * 1.2f, originalDiceObjectScale, 15f));
        }

        isRolling = false;
    }

    private IEnumerator ScaleObject(Transform target, Vector3 from, Vector3 to, float speed)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(from, to, easeT);
            yield return null;
        }
        target.localScale = to;
    }

    private void SpawnBurstParticles(Vector3 centerPosition)
    {
        if (resultDiceImage == null || canvasRect == null) return;
        Color originalColor = resultDiceImage.color;
        for (int i = 0; i < burstCount; i++)
        {
            GameObject particleObj = new GameObject("BurstParticle");
            particleObj.transform.SetParent(canvasRect, false);
            particleObj.transform.position = centerPosition;
            particleObj.transform.SetAsFirstSibling();
            Image particleImg = particleObj.AddComponent<Image>();
            particleImg.sprite = resultDiceImage.sprite;
            particleImg.color = originalColor;
            particleImg.raycastTarget = false;
            RectTransform pRect = particleObj.GetComponent<RectTransform>();
            pRect.sizeDelta = resultDiceImage.rectTransform.sizeDelta;
            pRect.localScale = Vector3.one * Random.Range(0.3f, 0.5f);
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomSpeed = Random.Range(200f, 350f);
            StartCoroutine(MoveAndFadeParticle(particleImg, pRect, randomDir, randomSpeed));
        }
    }

    private IEnumerator MoveAndFadeParticle(Image img, RectTransform rect, Vector2 direction, float speed)
    {
        float t = 0f;
        float currentSpeed = speed;
        float lifeTime = 1.0f;
        while (t < lifeTime)
        {
            rect.anchoredPosition += direction * currentSpeed * Time.deltaTime;
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 3f);
            Color c = img.color;
            c.a = Mathf.Lerp(1f, 0f, t / lifeTime);
            img.color = c;
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(img.gameObject);
    }

    private void UpdateKeyGuide(bool isMax)
    {
        if (keyGuideImage == null) return;
        bool isReadyToUse = (stats.currentDiceCharge >= 100f) && !isRolling;
        keyGuideImage.gameObject.SetActive(isReadyToUse);
        if (isReadyToUse && keyGuideSprites != null && keyGuideSprites.Length >= 2)
            keyGuideImage.sprite = isMax ? keyGuideSprites[1] : keyGuideSprites[0];
    }

    private void UpdateDiceFill(bool isMax)
    {
        if (diceFillImage == null) return;
        if (isMax)
        {
            diceFillImage.fillAmount = 1f;
            if (diceSprites != null && diceSprites.Length >= 2) diceFillImage.sprite = diceSprites[1];
        }
        else
        {
            diceFillImage.fillAmount = (stats.currentDiceCharge % 100f) / 100f;
            if (diceSprites != null && diceSprites.Length >= 2) diceFillImage.sprite = diceSprites[0];
        }
    }

    private void UpdateGaugeFill(bool isMax)
    {
        if (gaugeFillImage == null) return;
        gaugeFillImage.fillAmount = stats.currentDiceCharge / stats.maxDiceCharge;
        if (gaugeSprites != null && gaugeSprites.Length >= 2)
            gaugeFillImage.sprite = isMax ? gaugeSprites[1] : gaugeSprites[0];
    }

    private void UpdateText(bool isMax)
    {
        if (maxText == null) return;
        bool isReadyToUse = (stats.currentDiceCharge >= 100f) && !isRolling;
        maxText.SetActive(isMax && isReadyToUse);
        if (diceCountText == null) return;
        diceCountText.gameObject.SetActive(isReadyToUse);
        diceCountText.color = isMax ? textMaxColor : textNormalColor;
        int diceCount = Mathf.FloorToInt(stats.currentDiceCharge / 100f);
        if (diceCount > 3) diceCount = 3;
        diceCountText.text = diceCount.ToString();
    }
}