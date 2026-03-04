using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] dashFillImages;
    [SerializeField] private Color chargingColor = new Color(1, 1, 1, 0.5f); // 충전 중 색상
    [SerializeField] private Color fullColor = Color.white;                 // 완충 색상

    void Update()
    {
        float currentStacks = GameManager.instance.stats.currentDashStacks;

        for (int i = 0; i < dashFillImages.Length; i++)
        {
            // i번째 스택이 이미 꽉 찼는지, 아니면 지금 차오르는 중인지 판별
            if (i < (int)currentStacks)
            {
                dashFillImages[i].fillAmount = 1f; // 완충 상태
            }
            else if (i == (int)currentStacks)
            {
                dashFillImages[i].fillAmount = currentStacks % 1f; // 차오르는 상태
            }
            else
            {
                dashFillImages[i].fillAmount = 0f; // 아직 시작 전
            }
        }
    }
}