using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BalancePanel : MonoBehaviour
{
    public static BalancePanel instance; // 트리거들이 찾기 쉽게 싱글톤

    [Header("보여줄 메인 이미지")]
    public Image mainStarImage; // 실제 별 그림이 그려지는 Image 컴포넌트

    [Header("스프라이트 목록")]
    public Sprite normalSprite; // 아무것도 안 켰을 때 (기본)
    public List<Sprite> highlightSprites;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        ResetToNormal(); // 켜질 때 초기화
    }
    
    public void SetHighlight(int index)
    {
        if (mainStarImage != null && index >= 0 && index < highlightSprites.Count)
        {
            mainStarImage.sprite = highlightSprites[index];
        }
    }
    
    public void ResetToNormal()
    {
        if (mainStarImage != null && normalSprite != null)
        {
            mainStarImage.sprite = normalSprite;
        }
    }
}