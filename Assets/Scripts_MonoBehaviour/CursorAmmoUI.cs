using UnityEngine;
using TMPro;

public class CursorAmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoDisplayText;
    [SerializeField] private Color warningColor = Color.red;
    private Color normalColor;

    void Start()
    {
        if (ammoDisplayText != null) normalColor = ammoDisplayText.color;
    }

    void Update()
    {
        // 1. 현재 무기가 총일 때만 보여줌
        bool isGun = GameManager.instance.weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun;
        ammoDisplayText.gameObject.SetActive(isGun);

        if (!isGun) return;

        // 2. 탄약 수치 계산 (현재 500이 최대라면 5발분)
        int current = GameManager.instance.stats.currentAmmo / 100;
        int max = GameManager.instance.stats.maxAmmo / 100;

        ammoDisplayText.text = $"{current} / {max}";

        // 3. 한 발도 없을 때 색상 변경 피드백
        ammoDisplayText.color = (current <= 0) ? warningColor : normalColor;
    }
}