using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    public GameObject swordPanel;
    public GameObject gunPanel;
    public Image[] bulletPanels;
    public Sprite[] bulletSprites;
    public TextMeshProUGUI[] ammoText;

    private void Update()
    {
        for (int i = 0; i < bulletPanels.Length; i++)
        {
            if ((i * 100 + 100) <= GameManager.instance.Stats.currentAmmo)
            {
                bulletPanels[i].sprite = bulletSprites[1];
            }
            else
            {
                bulletPanels[i].sprite = bulletSprites[0];
            }
        }

        int currentAmmo = GameManager.instance.Stats.currentAmmo;

        ammoText[0].text = (currentAmmo / 100).ToString();
        ammoText[1].text = "." + (currentAmmo % 100).ToString("D2");
    }
}
