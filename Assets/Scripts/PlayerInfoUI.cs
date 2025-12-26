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
        healthSlider.value = GameManager.instance.Stats.currentHealth;
        healthText.text = GameManager.instance.Stats.currentHealth + " / " + GameManager.instance.Stats.maxHealth;

        manaSlider.value = GameManager.instance.Stats.currentMana;
        manaText.text = GameManager.instance.Stats.currentMana + " / " + GameManager.instance.Stats.maxMana;
    }
}
