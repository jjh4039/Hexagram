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
    [SerializeField] private ParticleSystem[] diceDesEffetParticles;
    [SerializeField] private ParticleSystemRenderer[] diceDesEffetParticleRenderers;

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
        diceDesEffetParticleRenderers[0].material = diceMaterials[diceValue];
        diceDesEffetParticleRenderers[1].material = diceMaterials[diceValue];
        
        diceDesTitle.color = diceColors[diceValue];

        for (int i = 0; i <= 100; i++)
        {
            diceDesAlpha.alpha = i * 0.01f;

            Color pColor = Color.white;
            pColor.a = i * 0.01f; // 루프 내의 알파값 적용

            var main = diceDesEffetParticles[0].main;
            var main2 = diceDesEffetParticles[1].main;

            main.startColor = new ParticleSystem.MinMaxGradient(pColor);
            main2.startColor = new ParticleSystem.MinMaxGradient(pColor);

            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(2f);


        for (int i = 100; i >= 0; i--)
        {
            diceDesAlpha.alpha = i * 0.01f;

            Color pColor = Color.white;
            pColor.a = i * 0.01f; // 루프 내의 알파값 적용

            var main = diceDesEffetParticles[0].main;
            var main2 = diceDesEffetParticles[1].main;

            main.startColor = new ParticleSystem.MinMaxGradient(pColor);
            main2.startColor = new ParticleSystem.MinMaxGradient(pColor);

            yield return new WaitForSeconds(0.01f);
        }
    }
}
