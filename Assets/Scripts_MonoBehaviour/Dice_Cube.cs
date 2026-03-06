using UnityEngine;

public class Dice_Cube : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private Vector3 rotationAxis = new Vector3(1, 0.8f, 1.2f); // ★ 불규칙한 회전축 설정

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 0.2f; // 위아래 이동 범위
    [SerializeField] private float floatFrequency = 1.5f; // 부유 속도

    private Vector3 startPos;

    void Awake()
    {
        // 시작 시점의 로컬 위치를 저장합니다.
        startPos = transform.localPosition;
    }

    void Update()
    {
        // 1. 불규칙한 회전 연출
        // 고정된 (1,1,1) 대신 미세하게 다른 축 값을 곱해 자이로스코프 느낌을 줍니다.
        transform.Rotate(rotationAxis * rotateSpeed * Time.deltaTime);

        // 2. 부유(Floating) 연출
        // Sin 곡선을 이용해 부드럽게 위아래로 움직이게 합니다.
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}