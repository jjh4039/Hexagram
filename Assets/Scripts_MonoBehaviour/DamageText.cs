using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color originalColor;

    [Header("Motion Settings")]
    [SerializeField] private float moveSpeed = 2f;    // 튀어 오르는 속도
    [SerializeField] private float fadeSpeed = 3f;    // 사라지는 속도 (알파값)
    [SerializeField] private float gravity = 2f;      // 떨어지는 중력 느낌

    [Header("Design Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.6f, 0f); // 주황색
    [SerializeField] private float normalSize = 4f;
    [SerializeField] private float criticalSize = 6f;

    private Vector3 moveVector;
    private float alpha = 1f;

    // 렌더링 순서 꼬임 방지용 (점점 증가)
    private static int globalSortingOrder = 2000;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    // ★ 적이 이 함수를 부를 때 '치명타 여부'도 같이 받음
    public void Setup(float damageAmount, bool isCritical)
    {
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        // 1. 색상 & 크기 설정
        if (isCritical)
        {
            textMesh.fontSize = criticalSize;
            textMesh.color = criticalColor;
            textMesh.fontStyle = FontStyles.Bold; // 치명타는 굵게!
        }
        else
        {
            textMesh.fontSize = normalSize;
            textMesh.color = normalColor;
            textMesh.fontStyle = FontStyles.Normal;
        }

        originalColor = textMesh.color;
        alpha = 1f;

        // 2. 튀어 오르는 방향 설정 (랜덤성 추가)
        // 좌우로 살짝(-0.5 ~ 0.5) 퍼지면서, 위로(1.0) 솟구침
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * moveSpeed;

        // 3. 맨 앞에 보이게 순서 정렬
        textMesh.sortingOrder = globalSortingOrder++;
        if (globalSortingOrder > 30000) globalSortingOrder = 2000; // 초기화
    }

    // 텍스트 띄우기 용 오버로드
    public void Setup(string message, Color color, float size)
    {
        textMesh.text = message;
        textMesh.color = color;
        textMesh.fontSize = size; // 요청하신 고정 사이즈 적용
        textMesh.fontStyle = FontStyles.Bold;

        originalColor = textMesh.color;
        alpha = 1f;

        // 동일한 연출 적용
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * moveSpeed;
        textMesh.sortingOrder = globalSortingOrder++;
        if (globalSortingOrder > 30000) globalSortingOrder = 2000;
    }

    void Update()
    {
        // 1. 이동 (위로 솟았다가 중력 때문에 천천히 떨어짐)
        transform.position += moveVector * Time.deltaTime;

        // y축 속도를 계속 줄임 (중력 효과) -> 솟구쳤다가 뚝 떨어지는 느낌
        moveVector.y -= gravity * Time.deltaTime;

        // 2. 서서히 사라지기 (Fade Out)
        // 생성되고 아주 잠깐 뒤부터 사라지기 시작
        alpha -= Time.deltaTime * fadeSpeed;

        // 색상 업데이트
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        // 3. 완전히 투명해지면 삭제
        if (alpha <= 0)
        {
            Destroy(gameObject);
        }
    }
}
