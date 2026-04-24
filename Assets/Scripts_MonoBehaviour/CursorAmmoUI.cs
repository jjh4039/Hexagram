using UnityEngine;
using TMPro;

public class CursorAmmoUI : MonoBehaviour
{
    [SerializeField] private VirtualCursor virtualCursor; // 가상 커서 스크립트 참조
    [SerializeField] private TextMeshProUGUI ammoDisplayText; // 탄약 수량을 표시할 텍스트
    [SerializeField] private Color warningColor = Color.red; // 탄약 부족 시 표시할 색상
    private Color normalColor; // 기본 텍스트 색상

    void Start()
    {
        if (ammoDisplayText != null) normalColor = ammoDisplayText.color;
    }

    void Update()
    {
        bool isAimMode = virtualCursor && virtualCursor.CurrentCursorType == CursorType.Aim; // 조준 모드 여부 확인
        bool isGun = GameManager.instance.weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun; // 총기 장착 여부 확인
        
        ammoDisplayText.gameObject.SetActive(isGun && isAimMode);

        if (!isGun || !isAimMode) return;

        int current = GameManager.instance.stats.currentAmmo / 100; // 현재 탄약 계산
        int max = GameManager.instance.stats.maxAmmo / 100; // 최대 탄약 계산

        ammoDisplayText.text = $"{current} / {max}";

        ammoDisplayText.color = (current <= 0) ? warningColor : normalColor;
    }
}