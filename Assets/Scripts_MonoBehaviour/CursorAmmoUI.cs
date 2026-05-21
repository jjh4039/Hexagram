using UnityEngine;
using TMPro;

public class CursorAmmoUI : MonoBehaviour
{
    [SerializeField] private VirtualCursor virtualCursor; 
    [SerializeField] private TextMeshProUGUI ammoDisplayText; 
    [SerializeField] private Color warningColor = Color.red; 
    private Color normalColor; 

    void Start()
    {
        if (ammoDisplayText != null) normalColor = ammoDisplayText.color;
    }

    void Update()
    {
        // ★ 수정: 게임매니저와 플레이어가 준비되지 않았을 때의 NullReference 에러 방어
        if (!GameManager.instance || !GameManager.instance.weaponManager || !GameManager.instance.stats) 
        {
            if (ammoDisplayText) ammoDisplayText.gameObject.SetActive(false);
            return;
        }

        bool isAimMode = virtualCursor && virtualCursor.CurrentCursorType == CursorType.Aim; 
        bool isGun = GameManager.instance.weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun; 
        
        ammoDisplayText.gameObject.SetActive(isGun && isAimMode);

        if (!isGun || !isAimMode) return;

        int current = GameManager.instance.stats.currentAmmo / 100; 
        int max = GameManager.instance.stats.maxAmmo / 100; 

        ammoDisplayText.text = $"{current} / {max}";

        ammoDisplayText.color = (current <= 0) ? warningColor : normalColor;
    }
}