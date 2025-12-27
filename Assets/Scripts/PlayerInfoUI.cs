using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    public Slider healthSlider;
    public Slider manaSlider;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;

    public void Update()
    {
        healthSlider.value = GameManager.instance.stats.currentHealth;
        healthText.text = GameManager.instance.stats.currentHealth + " / " + GameManager.instance.stats.maxHealth;

        manaSlider.value = GameManager.instance.stats.currentMana;
        manaText.text = GameManager.instance.stats.currentMana + " / " + GameManager.instance.stats.maxMana;
    }
}
