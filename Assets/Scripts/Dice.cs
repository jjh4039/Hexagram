using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using UnityEditor.Build;
using System.Runtime.InteropServices;
using System.Collections;

public class Dice : MonoBehaviour
{
    [SerializeField] private Color32[] diceColors;
    [SerializeField] private Material[] diceMaterials;

    [Header("Var")]
    private bool isNextRoll;
    private int diceValue;
    [SerializeField] private float diceTime;
    [SerializeField] private float rollTime;

    [Header("Dice Des")]
    [SerializeField] private CanvasGroup diceDesAlpha;
    [SerializeField] private TextMeshProUGUI diceDesTitle;
    [SerializeField] private TextMeshProUGUI diceDesSubtitle;
    [SerializeField] private Image diceDesImage;
    [SerializeField] private ParticleSystemRenderer[] diceDesEffets;

    [Header("Dice Module")]
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Image diceModuleImage;
    [SerializeField] private Image diceModuleBackGround;
    [SerializeField] private Slider diceModuleSlider;
    [SerializeField] private TextMeshProUGUI diceModuleText;

    private void Awake()
    {
        diceDesAlpha.alpha = 0f;
        isNextRoll = false;

        diceModuleSlider.value = 0;
        diceModuleSlider.maxValue = rollTime;
        diceModuleText.text = "주사위 값 재설정 중...";
        diceModuleBackGround.color = diceColors[6];
        diceModuleText.color = diceColors[6];
    }

    private void Update()
    {
        diceModuleSlider.value += Time.deltaTime;

        if (isNextRoll)
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(diceColors[diceValue]);
            diceModuleText.text = $"<color=#{hexColor}>[ {diceValue + 1} ] 모듈 발현 중</color> - {(diceTime - (int)diceModuleSlider.value)}s";
        }

        if (diceModuleSlider.value >= diceModuleSlider.maxValue)
        {
            RollDice(isNextRoll);
        }
    }

    private void RollDice(bool isRolling)
    {
        diceModuleSlider.value = 0;

        if (isRolling) // 모듈 굴리기
        {
            diceModuleSlider.maxValue = rollTime;
            diceModuleText.text = "주사위 값 재설정 중...";

            diceModuleImage.color = new Color(diceModuleImage.color.r, diceModuleImage.color.g, diceModuleImage.color.b, 0f);
            diceModuleBackGround.color = diceColors[6];
            diceModuleText.color = diceColors[6];
        }
        else  // 효과 받기
        { 
            diceValue = Random.Range(0, 6); // 주사위 값

            diceModuleSlider.maxValue = diceTime;
            diceModuleImage.sprite = diceSprites[diceValue];
            diceModuleImage.color = new Color(diceModuleImage.color.r, diceModuleImage.color.g, diceModuleImage.color.b, 1f);
            diceModuleBackGround.color = diceColors[diceValue];
            StartCoroutine("DiceDes", diceValue);
        }

        isNextRoll = !isNextRoll; // 상태 전환
    }

    IEnumerator DiceDes(int diceValue)
    {
        diceDesEffets[0].material = diceMaterials[diceValue];
        diceDesEffets[1].material = diceMaterials[diceValue];

        var uiPart1 = diceDesEffets[0].GetComponent<Coffee.UIExtensions.UIParticle>();
        var uiPart2 = diceDesEffets[1].GetComponent<Coffee.UIExtensions.UIParticle>();

        if (uiPart1 != null)
        {
            uiPart1.enabled = false;
            uiPart1.enabled = true;
        }
        if (uiPart2 != null)
        {
            uiPart2.enabled = false;
            uiPart2.enabled = true;
        }

        diceDesEffets[0].gameObject.GetComponent<ParticleSystem>().Clear();
        diceDesEffets[0].gameObject.GetComponent<ParticleSystem>().Play();
        diceDesEffets[1].gameObject.GetComponent<ParticleSystem>().Clear();
        diceDesEffets[1].gameObject.GetComponent<ParticleSystem>().Play();

        diceDesTitle.color = diceColors[diceValue];

        for (int i = 0; i <= 100; i++)
        {
            float alphaVal = i * 0.02f;
            diceDesAlpha.alpha = alphaVal;

            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(2f);

        for (int i = 100; i >= 0; i--)
        {
            float alphaVal = i * 0.01f;
            diceDesAlpha.alpha = alphaVal;

            yield return new WaitForSeconds(0.01f);
        }
    }
}
