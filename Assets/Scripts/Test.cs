using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public TextMeshProUGUI diceText;
    public Slider diceSlider;
    public Image diceSliderBack;
    public bool isDiceOn;

    public void Start()
    {
        diceSlider.value = 0;
    }

    public void Update()
    {
        diceSlider.value += Time.deltaTime;

        if (isDiceOn)
        {
            diceSlider.maxValue = 10;
            diceText.text = "모듈 발동 : " + ((10) - (int)diceSlider.value) + "s";
            diceText.color = new Color32(255, 83, 83, 255);
            diceSliderBack.color = new Color32(255, 83, 83, 255);
        }
        else
        {
            diceSlider.maxValue = 3;
            diceText.text = "주사위 값 재설정 중...";
            diceText.color = new Color32(96, 96, 96, 255);
            diceSliderBack.color = new Color32(96, 96, 96, 255);
        }


        if (diceSlider.value >= diceSlider.maxValue)
        {
            diceSlider.value = 0;
            isDiceOn = !isDiceOn;
            diceSliderBack.color = new Color32(96, 96, 96, 255);
        }
    }
}
