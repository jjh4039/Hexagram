using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    public void Update()
    {
        healthSlider.value = GameManager.instance.stats.currentHealth;
        healthText.text = GameManager.instance.stats.currentHealth + " / " + GameManager.instance.stats.maxHealth;
    }
}
