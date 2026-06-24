using UnityEngine;

public class HangingUI : MonoBehaviour
{
    [Header("물리 설정")] [SerializeField] private float gravity = 30f; // 복원력 (클수록 빨리 멈춤)
    [SerializeField] private float drag = 0.98f; // 공기 저항 (0.9 ~ 0.99)
    [SerializeField] private float maxAngle = 10f; // 최대 회전 각도 제한

    [Header("초기 연출")] [SerializeField] private float startPushForce = 15f; // 창 열릴 때 미는 힘

    private float currentVelocity = 0f; // 현재 회전 속도
    private float currentAngle = 0f; // 현재 각도
    private RectTransform rectTran;

    private void Awake()
    {
        rectTran = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        currentAngle = 0f;
        currentVelocity = startPushForce;
    }

    private void Update()
    {
        if (Mathf.Abs(currentVelocity) < 0.01f && Mathf.Abs(currentAngle) < 0.01f)
        {
            if (currentAngle != 0)
            {
                currentAngle = 0;
                rectTran.localRotation = Quaternion.identity;
            }

            return;
        }

        float restorationForce = -currentAngle * gravity * Time.unscaledDeltaTime;

        currentVelocity += restorationForce;
        currentVelocity *= drag;
        currentAngle += currentVelocity * Time.unscaledDeltaTime;
        currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);

        rectTran.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }


    public void Push(float force)
    {
        currentVelocity += force;
    }
}