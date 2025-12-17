using UnityEngine;

public class Sword_Effect : MonoBehaviour
{
    [SerializeField] private float duration; // 이펙트가 머무는 시간

    private void OnEnable()
    {
        // 켜지는 순간 카운트다운 시작
        CancelInvoke();
        Invoke("Disable", duration);
    }

    private void Disable()
    {
        gameObject.SetActive(false);
    }
}
