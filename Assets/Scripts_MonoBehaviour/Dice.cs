using UnityEngine;
using UnityEngine.InputSystem;

public class Dice : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Dice_UI diceUI;

    [Header("Data Settings")]
    [SerializeField] public DiceData[] diceList;
    [SerializeField] public DiceData defaultData;

    private void Update()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null || GameManager.instance.player == null)
            return;

        PlayerStats stats = GameManager.instance.stats;
        Player player = GameManager.instance.player;

        HandleInput(stats, player);
    }

    private void HandleInput(PlayerStats stats, Player player)
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (diceUI != null && !diceUI.IsRolling && stats.currentDiceCharge >= 100f)
        {
            RollDice(stats, player);
        }
    }

    private void RollDice(PlayerStats stats, Player player)
    {
        stats.currentDiceCharge -= 100f;
        stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);

        if (diceList == null || diceList.Length == 0)
            return;

        int randomIndex = Random.Range(0, diceList.Length);
        DiceData selectedData = diceList[randomIndex];

        if (diceUI != null && selectedData != null)
        {
            diceUI.PlayRollAnimation(selectedData, randomIndex, () =>
            {
                if (player != null)
                {
                    player.ApplyDiceBuff(selectedData);
                }
            });
        }
    }

    public void AddChargeFromHit()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null)
            return;

        GameManager.instance.stats.AddDiceChargeFromHit();
    }
}