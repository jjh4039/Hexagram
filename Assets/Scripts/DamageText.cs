using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1f; // 올라가는 속도
    [SerializeField] float destroyTime = 0.6f; // 사라지는 시간
    private TextMeshPro textMesh;

    private static int sortOrder = 20; // 텍스트가 겹칠 때를 대비한 렌더링 순서

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    // 적이 이 함수를 부를 거야 (숫자 설정)
    public void Setup(float damageAmount)
    {
        // 숫자를 글자로 바꿔서 넣기
        textMesh.text = damageAmount.ToString();

        textMesh.sortingOrder = sortOrder; 
        sortOrder++; 
    }

    void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
    }

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
