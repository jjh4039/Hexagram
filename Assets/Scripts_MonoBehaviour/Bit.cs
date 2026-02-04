using UnityEngine;
using System.Collections;

public class Bit : MonoBehaviour
{ 
    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    public void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (keyGuide != null) keyGuide.SetActive(false); // 처음엔 'F' 표시 끄기
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            spriteRenderer.material = outLineMaterial[1]; // 빛나는 재질로 변경
            if (keyGuide != null) keyGuide.SetActive(true); // 'F' 표시 켜기
        }
    }

    // 플레이어가 멀어졌을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            spriteRenderer.material = outLineMaterial[0]; // 기본 재질로 변경
            if (keyGuide != null) keyGuide.SetActive(false); // 'F' 표시 끄기
        }
    }
}
