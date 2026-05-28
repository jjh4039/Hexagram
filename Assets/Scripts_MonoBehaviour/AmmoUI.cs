using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AmmoUI : MonoBehaviour
{
    [Header("Ammo Visuals")] 
    public Image[] bulletPanels;
    public TextMeshProUGUI[] ammoText;

    [Header("Ammo Sprites (New)")] 
    [SerializeField] private Sprite normalAmmoSprite;
    [SerializeField] private Sprite maxAmmoSprite;

    [Header("Scrap Visuals")] 
    [SerializeField] private RectTransform scrapGroup;
    [SerializeField] private TextMeshProUGUI scrapText;
    [SerializeField] private float scrapMoveDistance = 50f;
    
    private Vector2 _scrapOriginPos;
    private Vector3 _scrapTextOriginScale;
    private Coroutine _scrapPunchRoutine;

    [Header("Ammo Status Colors")] 
    [SerializeField] private Color emptyTextColor = Color.red; // 0~99
    [SerializeField] private Color normalTextColor = Color.white; // 100~499
    [SerializeField] private Color maxAmmoTextColor = new Color(1f, 0.5f, 0f); // 500

    [Header("Case Visuals")] 
    [SerializeField] private Image[] caseImages;
    [SerializeField] private Color caseNormalColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Color caseAimingColor = new Color(0, 0, 0, 0.8f);

    [Header("Aiming Animation")] 
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float aimingScale = 1.2f;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private RectTransform uiRoot;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxAmmoFull;

    private Coroutine _scaleCoroutine;
    private bool _lastAimingState;
    private int _lastScrapCount = -1;
    private int _lastAmmoCount = -1; // 총알 충전 감지용

    private void Awake()
    {
        if (uiRoot == null) uiRoot = GetComponent<RectTransform>();
        if (scrapText != null) _scrapTextOriginScale = scrapText.transform.localScale;
        if (scrapGroup != null) _scrapOriginPos = scrapGroup.anchoredPosition;

        if (normalAmmoSprite == null && bulletPanels.Length > 0) normalAmmoSprite = bulletPanels[0].sprite;
    }

    private void Update()
    {
        if (!GameManager.instance || !GameManager.instance.weaponManager) return;

        UpdateAmmoVisuals();
        UpdateScrapVisuals();

        bool currentAiming = GameManager.instance.weaponManager.IsAiming;
        if (currentAiming != _lastAimingState)
        {
            _lastAimingState = currentAiming;
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(Co_ScaleAnimation(currentAiming));
        }
    }

    private void UpdateAmmoVisuals()
    {
        int currentAmmo = GameManager.instance.stats.currentAmmo;
        int maxAmmoValue = GameManager.instance.stats.maxAmmo;
        bool isMax = currentAmmo >= maxAmmoValue;

        // 총알이 꽉 차는 순간 사운드 재생 (게임 시작 시 최초 1회는 방지)
        if (currentAmmo != _lastAmmoCount)
        {
            if (isMax && _lastAmmoCount != -1 && _lastAmmoCount < maxAmmoValue)
            {
                if (sfxAmmoFull != null && SoundManager.instance != null)
                {
                    SoundManager.instance.PlaySFX(sfxAmmoFull, 0.8f);
                }
            }
            _lastAmmoCount = currentAmmo;
        }

        ammoText[0].text = (currentAmmo / 100).ToString();
        ammoText[1].text = "." + (currentAmmo % 100).ToString("D2");

        Color targetTextColor;

        if (currentAmmo < 100) targetTextColor = emptyTextColor;
        else if (isMax) targetTextColor = maxAmmoTextColor;
        else targetTextColor = normalTextColor;

        ammoText[0].color = targetTextColor;
        ammoText[1].color = targetTextColor;

        if (ammoText.Length > 2 && ammoText[2])
        {
            ammoText[2].gameObject.SetActive(isMax);
            if (isMax) ammoText[2].color = maxAmmoTextColor;
        }

        Sprite targetAmmoSprite = isMax ? maxAmmoSprite : normalAmmoSprite;

        for (int i = 0; i < bulletPanels.Length; i++)
        {
            int threshold = (i * 100 + 100);

            if (threshold <= currentAmmo) bulletPanels[i].fillAmount = 1f;
            else if (currentAmmo > threshold - 100) bulletPanels[i].fillAmount = (currentAmmo % 100) / 100f;
            else bulletPanels[i].fillAmount = 0f;

            if (bulletPanels[i].sprite != targetAmmoSprite)
            {
                bulletPanels[i].sprite = targetAmmoSprite;
            }
        }
    }

    private IEnumerator Co_ScaleAnimation(bool isAiming)
    {
        Vector3 startScale = uiRoot.localScale;
        Vector3 targetScale = Vector3.one * (isAiming ? aimingScale : normalScale);
        Vector2 startScrapPos = scrapGroup.anchoredPosition;
        Vector2 targetScrapPos = isAiming ? _scrapOriginPos + new Vector2(scrapMoveDistance, 0) : _scrapOriginPos;
        Color startCaseColor = caseImages.Length > 0 ? caseImages[0].color : Color.white;
        Color targetCaseColor = isAiming ? caseAimingColor : caseNormalColor;
        float elapsed = 0f;
        
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float curve = 1f - Mathf.Pow(1f - t, 3); 
            
            uiRoot.localScale = Vector3.Lerp(startScale, targetScale, curve);
            if (scrapGroup) scrapGroup.anchoredPosition = Vector2.Lerp(startScrapPos, targetScrapPos, curve);
            
            foreach (var img in caseImages)
            {
                if (img) img.color = Color.Lerp(startCaseColor, targetCaseColor, curve);
            }

            yield return null;
        }

        uiRoot.localScale = targetScale;
        if (scrapGroup) scrapGroup.anchoredPosition = targetScrapPos;
        foreach (var img in caseImages)
            if (img) img.color = targetCaseColor;
    }

    private void UpdateScrapVisuals()
    {
        if (!scrapText) return;
        int currentScrap = GameManager.instance.currentScrap;
        if (currentScrap != _lastScrapCount)
        {
            scrapText.text = currentScrap.ToString();
            if (_lastScrapCount != -1)
            {
                if (_scrapPunchRoutine != null) StopCoroutine(_scrapPunchRoutine);
                _scrapPunchRoutine = StartCoroutine(Co_ScrapPunch());
            }

            _lastScrapCount = currentScrap;
        }
    }

    private IEnumerator Co_ScrapPunch()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        Vector3 targetScale = _scrapTextOriginScale * 1.15f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI);
            scrapText.transform.localScale = Vector3.Lerp(_scrapTextOriginScale, targetScale, scale);
            yield return null;
        }

        scrapText.transform.localScale = _scrapTextOriginScale;
    }
}