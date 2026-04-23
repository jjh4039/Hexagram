using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    public Slider healthSlider; // 화면에 표시되는 체력바
    public TextMeshProUGUI healthText; // 현재 체력과 최대 체력 문자

    public void Update()
    {
        healthSlider.maxValue = GameManager.instance.stats.maxHealth;
        healthSlider.value = GameManager.instance.stats.currentHealth;
        healthText.text = GameManager.instance.stats.currentHealth + " / " + GameManager.instance.stats.maxHealth;
    }
}