using UnityEngine;

public class Dice_Cube : MonoBehaviour
{
    [Header("--- Roll Settings ---")]
    [SerializeField] private float rollSpeed = 1000f;
    [SerializeField] private float minRotationThreshold = 0.5f; // 축의 최소 값 보정치  

    private Vector3 currentAxis;

    void OnEnable()
    {
        Vector3 rawAxis = Random.onUnitSphere;

        if (Mathf.Abs(rawAxis.x) < minRotationThreshold) rawAxis.x = minRotationThreshold * Mathf.Sign(rawAxis.x == 0 ? 1 : rawAxis.x);
        if (Mathf.Abs(rawAxis.y) < minRotationThreshold) rawAxis.y = minRotationThreshold * Mathf.Sign(rawAxis.y == 0 ? 1 : rawAxis.y);
        if (Mathf.Abs(rawAxis.z) < minRotationThreshold) rawAxis.z = minRotationThreshold * Mathf.Sign(rawAxis.z == 0 ? 1 : rawAxis.z);

        currentAxis = rawAxis.normalized;
    }

    void Update()
    {
        // 월드 좌표 기준 회전
        transform.Rotate(currentAxis * (rollSpeed * Time.deltaTime), Space.World);

        // 로컬 좌표 기준 추가 회전 (덜그럭거리는 느낌)
        transform.Rotate(new Vector3(1.2f, 0.5f, 0.8f) * (rollSpeed * 0.5f * Time.deltaTime), Space.Self);
    }
}