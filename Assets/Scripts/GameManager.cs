using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;
    public PlayerStats Stats;
    public WeaponUI weaponUI;
    public Dice dice;

    void Awake()
    {
        instance = this;
    }
}
    