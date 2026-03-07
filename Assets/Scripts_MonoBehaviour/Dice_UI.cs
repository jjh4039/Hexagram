using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Dice_UI : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private Image diceFillImage;    // 주사위 모양의 Fill 스프라이트
    [SerializeField] private Image gaugeFillImage;   // 하단 3칸 게이지의 Fill 스프라이트
    [SerializeField] private Image keyGuideImage;    // Q 또는 E 키 가이드 이미지
    [SerializeField] private TextMeshProUGUI diceCountText; // 주사위 개수 텍스트
    [SerializeField] private GameObject maxText;     // MAX 텍스트 게임오브젝트 (추가)

    [Header("--- Sprites (0: Normal, 1: Max) ---")]
    [Tooltip("0번: 일반 상태(0~299), 1번: 최대 충전 상태(300)")]
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Sprite[] gaugeSprites;
    [SerializeField] private Sprite[] keyGuideSprites;

    [Header("--- Text Colors ---")]
    [SerializeField] private Color textNormalColor = Color.white;
    [SerializeField] private Color textMaxColor = Color.yellow;

    private PlayerStats stats;

    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.stats != null)
        {
            stats = GameManager.instance.stats;
        }
    }

    void Update()
    {
        if (stats == null) return;

        // Max 상태인지 한 번만 계산해서 각 함수에 전달
        bool isMax = stats.currentDiceCharge >= stats.maxDiceCharge;

        UpdateDiceFill(isMax);
        UpdateKeyGuide(isMax);
        UpdateGaugeFill(isMax);
        UpdateText(isMax);
    }

    // 1. 주사위 본체 차오르는 연출 및 스프라이트 교체
    private void UpdateDiceFill(bool isMax)
    {
        if (diceFillImage == null) return;

        if (isMax)
        {
            diceFillImage.fillAmount = 1f;
            if (diceSprites != null && diceSprites.Length >= 2) diceFillImage.sprite = diceSprites[1];
        }
        else
        {
            diceFillImage.fillAmount = (stats.currentDiceCharge % 100f) / 100f;
            if (diceSprites != null && diceSprites.Length >= 2) diceFillImage.sprite = diceSprites[0];
        }
    }

    // 2. 키 가이드 활성화 및 스프라이트 교체
    private void UpdateKeyGuide(bool isMax)
    {
        if (keyGuideImage == null) return;

        // 1스택(100) 이상일 때만 활성화
        bool isReadyToUse = stats.currentDiceCharge >= 100f;
        keyGuideImage.gameObject.SetActive(isReadyToUse);

        // 활성화 상태일 때 스프라이트 교체
        if (isReadyToUse && keyGuideSprites != null && keyGuideSprites.Length >= 2)
        {
            keyGuideImage.sprite = isMax ? keyGuideSprites[1] : keyGuideSprites[0];
        }
    }

    // 3. 하단 게이지 연출 및 스프라이트 교체
    private void UpdateGaugeFill(bool isMax)
    {
        if (gaugeFillImage == null) return;

        gaugeFillImage.fillAmount = stats.currentDiceCharge / stats.maxDiceCharge;

        if (gaugeSprites != null && gaugeSprites.Length >= 2)
        {
            gaugeFillImage.sprite = isMax ? gaugeSprites[1] : gaugeSprites[0];
        }
    }

    // 4. 주사위 개수 텍스트 및 MAX 텍스트 제어
    private void UpdateText(bool isMax)
    {
        // 300일 때 MAX 오브젝트 켜기, 아니면 끄기
        if (maxText != null)
        {
            maxText.SetActive(isMax);
        }

        if (diceCountText == null) return;

        diceCountText.color = isMax ? textMaxColor : textNormalColor;

        int diceCount = Mathf.FloorToInt(stats.currentDiceCharge / 100f);
        if (diceCount > 3) diceCount = 3;

        diceCountText.text = diceCount.ToString();
    }
}