using UnityEngine;

public class HangingUI : MonoBehaviour
{
    [Header("물리 설정")]
    [SerializeField] private float gravity = 30f;      // 복원력 (클수록 빨리 멈춤)
    [SerializeField] private float drag = 0.98f;       // 공기 저항 (0.9 ~ 0.99)
    [SerializeField] private float maxAngle = 10f;     // 최대 회전 각도 제한

    [Header("초기 연출")]
    [SerializeField] private float startPushForce = 15f; // 창 열릴 때 미는 힘

    private float currentVelocity = 0f; // 현재 회전 속도
    private float currentAngle = 0f;    // 현재 각도
    private RectTransform rectTran;

    private void Awake()
    {
        rectTran = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // UI가 켜질 때(Tab 누름) 살짝 기울어진 상태로 시작하거나 밀어줌
        currentAngle = 0f;
        currentVelocity = startPushForce; // 툭 쳐서 시작
    }

    private void Update()
    {
        if (Mathf.Abs(currentVelocity) < 0.01f && Mathf.Abs(currentAngle) < 0.01f)
        {
            // 완벽한 정지 상태로 고정하고 리턴
            if (currentAngle != 0) {
                currentAngle = 0;
                rectTran.localRotation = Quaternion.identity;
            }
            return; 
        }
        
        // 1. 물리 계산 (단진자 운동 흉내)
        // 각도가 0으로 돌아가려는 힘 (중력)
        float restorationForce = -currentAngle * gravity * Time.unscaledDeltaTime;

        // 속도에 힘 더하기
        currentVelocity += restorationForce;

        // 공기 저항 적용 (점점 느려짐)
        currentVelocity *= drag;

        // 2. 각도 적용
        currentAngle += currentVelocity * Time.unscaledDeltaTime;

        // (선택사항) 너무 과하게 돌지 않게 제한
        currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);

        // 3. 실제 회전 시키기 (Z축 회전)
        rectTran.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }


    public void Push(float force)
    {
        currentVelocity += force;
    }
}