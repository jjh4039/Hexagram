using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 0.2f; // 부유 진폭
    [SerializeField] private float floatSpeed = 2f;       // 부유 속도

    private Vector3 _startPos;                            // 초기 위치 저장

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }
}