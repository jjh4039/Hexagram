using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageText : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color originalColor;

    [Header("Motion Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float gravity = 2f;

    [Header("Design Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private float normalSize = 4f;
    [SerializeField] private float criticalSize = 6f;

    private Vector3 moveVector;
    private float alpha = 1f;

    private static int globalSortingOrder = 2000;

    private static Queue<DamageText> pool = new Queue<DamageText>();
    private static Transform poolContainer; 

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public static DamageText Spawn(GameObject prefab, Vector3 position)
    {
        // ★ 수정: Find를 쓰지 않고, 그냥 최상단에 전용 폴더를 하나 만듭니다.
        if (poolContainer == null)
        {
            poolContainer = new GameObject("DamageText_Pool").transform;
        }

        DamageText dt;
        if (pool.Count > 0)
        {
            dt = pool.Dequeue();
            dt.transform.position = position;
            dt.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, poolContainer);
            dt = obj.GetComponent<DamageText>();
        }
        return dt;
    }

    public void Setup(float damageAmount, bool isCritical)
    {
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCritical)
        {
            textMesh.fontSize = criticalSize;
            textMesh.color = criticalColor;
            textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            textMesh.fontSize = normalSize;
            textMesh.color = normalColor;
            textMesh.fontStyle = FontStyles.Normal;
        }

        originalColor = textMesh.color;
        alpha = 1f;

        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * moveSpeed;

        textMesh.sortingOrder = globalSortingOrder++;
        if (globalSortingOrder > 30000) globalSortingOrder = 2000;
    }

    public void Setup(string message, Color color, float size)
    {
        textMesh.text = message;
        textMesh.color = color;
        textMesh.fontSize = size;
        textMesh.fontStyle = FontStyles.Bold;

        originalColor = textMesh.color;
        alpha = 1f;

        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * moveSpeed;
        textMesh.sortingOrder = globalSortingOrder++;
        if (globalSortingOrder > 30000) globalSortingOrder = 2000;
    }

    void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector.y -= gravity * Time.deltaTime;

        alpha -= Time.deltaTime * fadeSpeed;
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (alpha <= 0)
        {
            gameObject.SetActive(false);
            pool.Enqueue(this);
        }
    }
}