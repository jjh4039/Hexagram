    using UnityEngine;

    public enum DiceEffectType
    {
        AttackBuff,
        StrongAttackBuff,
        CritDamageBuff,
        Heal,
        SpeedBuff,
        RangedMegaBuff
    }

    [CreateAssetMenu(fileName = "New Dice Data", menuName = "Hexagram/DiceData")]
    public class DiceData : ScriptableObject
    {
        [Header("Info")]
        public string diceName;
        [TextArea] public string description;
        [TextArea] public string shortDescription;

        [Header("Visual")]
        public Sprite icon;
        public Color32 particleColor;
        public Color32 uiGlowColor;

        [Header("Effect")]
        public DiceEffectType effectType;
        public float effectValue;
        public float duration = 5f;
        public float secondaryValue = 0f;

        [Header("Weapon Visual")]
        public Material muzzleFlashMaterial;
    }