using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dice : MonoBehaviour
{
    [Header("--- UI Reference ---")]
    [SerializeField] private Dice_UI diceUI;

    [Header("--- Data Settings ---")]
    [SerializeField] public DiceData[] diceList;
    [SerializeField] public DiceData defaultData;

    private void Update()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;
        PlayerStats stats = GameManager.instance.stats;

        HandleInput(stats);
    }

    private void HandleInput(PlayerStats stats)
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // UI가 연출 중이 아닐 때만 실행 가능
            if (diceUI && !diceUI.IsRolling && stats.currentDiceCharge >= 100f)
            {
                RollDice(stats);
            }
        }
    }

    private void RollDice(PlayerStats stats)
    {
        // [로직] 게이지 즉시 차감
        stats.currentDiceCharge -= 100f;

        // [로직] 랜덤 결과 추출
        int randomIndex = Random.Range(0, diceList.Length);
        DiceData selectedData = diceList[randomIndex];

        // [로직] 실제 플레이어 버프 적용 (이곳에 버프 함수 호출 추가 가능)
        // ApplyBuff(selectedData);

        // [비주얼] UI에게 데이터 전달 및 연출 명령
        if (diceUI != null)
        {
            diceUI.PlayRollAnimation(selectedData, randomIndex);
        }
    }

    public void AddChargeFromHit()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;
        GameManager.instance.stats.AddDiceChargeFromHit();
    }
}