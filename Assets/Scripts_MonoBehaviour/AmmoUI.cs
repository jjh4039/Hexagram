using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AmmoUI : MonoBehaviour
{
    [Header("Ammo Visuals")] public Image[] bulletPanels;
    public TextMeshProUGUI[] ammoText;

    [Header("Ammo Sprites (New)")] [SerializeField]
    private Sprite normalAmmoSprite;

    [SerializeField] private Sprite maxAmmoSprite; 

    [Header("Scrap Visuals")] [SerializeField]
    private RectTransform scrapGroup;

    [SerializeField] private TextMeshProUGUI scrapText;
    [SerializeField] private float scrapMoveDistance = 50f;
    private Vector2 scrapOriginPos;
    private Vector3 scrapTextOriginScale;
    private Coroutine scrapPunchRoutine;

    [Header("Ammo Status Colors")] [SerializeField]
    private Color emptyTextColor = Color.red; // 0~99 (�ؽ�Ʈ ����)

    [SerializeField] private Color normalTextColor = Color.white; // 100~499
    [SerializeField] private Color maxAmmoTextColor = new Color(1f, 0.5f, 0f); // 500

    [Header("Case Visuals")] [SerializeField]
    private Image[] caseImages;

    [SerializeField] private Color caseNormalColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Color caseAimingColor = new Color(0, 0, 0, 0.8f);

    [Header("Aiming Animation")] [SerializeField]
    private float normalScale = 1.0f;

    [SerializeField] private float aimingScale = 1.2f;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private RectTransform uiRoot;

    private Coroutine scaleCoroutine;
    private bool lastAimingState;
    private int lastScrapCount = -1;

    private void Awake()
    {
        if (uiRoot == null) uiRoot = GetComponent<RectTransform>();
        if (scrapText != null) scrapTextOriginScale = scrapText.transform.localScale;
        if (scrapGroup != null) scrapOriginPos = scrapGroup.anchoredPosition;
        
        if (normalAmmoSprite == null && bulletPanels.Length > 0) normalAmmoSprite = bulletPanels[0].sprite;
    }

    private void Update()
    {
        if (GameManager.instance == null || GameManager.instance.weaponManager == null) return;

        UpdateAmmoVisuals();
        UpdateScrapVisuals();

        bool currentAiming = GameManager.instance.weaponManager.IsAiming;
        if (currentAiming != lastAimingState)
        {
            lastAimingState = currentAiming;
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(Co_ScaleAnimation(currentAiming));
        }
    }

    private void UpdateAmmoVisuals()
    {
        int currentAmmo = GameManager.instance.stats.currentAmmo;
        int maxAmmoValue = GameManager.instance.stats.maxAmmo;
        
        ammoText[0].text = (currentAmmo / 100).ToString();
        ammoText[1].text = "." + (currentAmmo % 100).ToString("D2");

        bool isMax = currentAmmo >= maxAmmoValue;
        Color targetTextColor;

        if (currentAmmo < 100) targetTextColor = emptyTextColor;
        else if (isMax) targetTextColor = maxAmmoTextColor;
        else targetTextColor = normalTextColor;

        ammoText[0].color = targetTextColor;
        ammoText[1].color = targetTextColor;

        if (ammoText.Length > 2 && ammoText[2] != null)
        {
            ammoText[2].gameObject.SetActive(isMax);
            if (isMax) ammoText[2].color = maxAmmoTextColor;
        }
        
        Sprite targetAmmoSprite = isMax ? maxAmmoSprite : normalAmmoSprite;

        for (int i = 0; i < bulletPanels.Length; i++)
        {
            int threshold = (i * 100 + 100);

            // �̹��� ä��� ���� (Vertical Fill)
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
        Vector2 targetScrapPos = isAiming ? scrapOriginPos + new Vector2(scrapMoveDistance, 0) : scrapOriginPos;
        Color startCaseColor = caseImages.Length > 0 ? caseImages[0].color : Color.white;
        Color targetCaseColor = isAiming ? caseAimingColor : caseNormalColor;
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float curve = 1f - Mathf.Pow(1f - t, 3); // Ease-Out
            uiRoot.localScale = Vector3.Lerp(startScale, targetScale, curve);
            if (scrapGroup != null) scrapGroup.anchoredPosition = Vector2.Lerp(startScrapPos, targetScrapPos, curve);
            foreach (var img in caseImages)
            {
                if (img != null) img.color = Color.Lerp(startCaseColor, targetCaseColor, curve);
            }

            yield return null;
        }

        uiRoot.localScale = targetScale;
        if (scrapGroup != null) scrapGroup.anchoredPosition = targetScrapPos;
        foreach (var img in caseImages)
            if (img != null)
                img.color = targetCaseColor;
    }

    private void UpdateScrapVisuals()
    {
        if (scrapText == null) return;
        int currentScrap = GameManager.instance.currentScrap;
        if (currentScrap != lastScrapCount)
        {
            scrapText.text = currentScrap.ToString();
            if (lastScrapCount != -1)
            {
                if (scrapPunchRoutine != null) StopCoroutine(scrapPunchRoutine);
                scrapPunchRoutine = StartCoroutine(Co_ScrapPunch());
            }

            lastScrapCount = currentScrap;
        }
    }

    private IEnumerator Co_ScrapPunch()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        Vector3 targetScale = scrapTextOriginScale * 1.15f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI);
            scrapText.transform.localScale = Vector3.Lerp(scrapTextOriginScale, targetScale, scale);
            yield return null;
        }

        scrapText.transform.localScale = scrapTextOriginScale;
    }
}