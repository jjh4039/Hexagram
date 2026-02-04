using ChocDino.UIFX;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BitManager : MonoBehaviour
{
    [SerializeField] private BitChoices[] bitChoices;
    [SerializeField] private ArtifactData[] allArtifacts;
    [SerializeField] private ArtifactGradeProbability gradeProbability;
    [SerializeField] private ArtifactGradeColor gradeColors;

    private HashSet<ArtifactData> usedArtifacts = new HashSet<ArtifactData>();

    void Start()
    {
        SetupBitChoices();
    }

    public void SetupBitChoices()
    {
        usedArtifacts.Clear();

        for (int i = 0; i < bitChoices.Length; i++)
        {
            ArtifactData artifact = GetRandomArtifactByProbability();

            if (artifact != null)
            {
                usedArtifacts.Add(artifact);
            }

            ApplyArtifactToChoice(bitChoices[i], artifact);
        }
    }

    private ArtifactData GetRandomArtifactByProbability()
    {
        ArtifactGrade grade = GetRandomGrade();
        return GetRandomArtifactByGrade(grade);
    }

    private ArtifactGrade GetRandomGrade()
    {
        float total =
            gradeProbability.common +
            gradeProbability.rare +
            gradeProbability.epic +
            gradeProbability.legendary;

        float rand = Random.value * total;

        if (rand < gradeProbability.common)
            return ArtifactGrade.Common;

        rand -= gradeProbability.common;
        if (rand < gradeProbability.rare)
            return ArtifactGrade.Rare;

        rand -= gradeProbability.rare;
        if (rand < gradeProbability.epic)
            return ArtifactGrade.Epic;

        return ArtifactGrade.Legendary;
    }

    private ArtifactData GetRandomArtifactByGrade(ArtifactGrade grade)
    {
        List<ArtifactData> candidates = new List<ArtifactData>();

        foreach (var artifact in allArtifacts)
        {
            if (artifact.grade != grade)
                continue;

            if (usedArtifacts.Contains(artifact))
                continue;

            candidates.Add(artifact);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No available Artifact for grade : " + grade);
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void ApplyArtifactToChoice(BitChoices choice, ArtifactData artifact)
    {
        if (artifact == null)
            return;

        choice.artifactImage.sprite = artifact.icon;
        choice.titleText.text = artifact.artifactName;
        choice.gradeText.text = artifact.grade.ToString();
        choice.desText.text = artifact.description;

        for (int i = 0; i < choice.gradeEffects.Length; i++)
        {
            choice.gradeEffects[i].Color = GetColorByGrade(artifact.grade);
        }
    }

    private Color GetColorByGrade(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Common:
                return gradeColors.common;
            case ArtifactGrade.Rare:
                return gradeColors.rare;
            case ArtifactGrade.Epic:
                return gradeColors.epic;
            case ArtifactGrade.Legendary:
                return gradeColors.legendary;
            default:
                return Color.white;
        }
    }

    [System.Serializable]
    public struct BitChoices
    {
        public RectTransform rect;
        public CanvasGroup group;
        public GlowFilter choiceEffect;
        public GlowFilter[] gradeEffects;

        public Image artifactImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI gradeText;
        public TextMeshProUGUI desText;
    }

    [System.Serializable]
    public struct ArtifactGradeProbability
    {
        [Range(0f, 1f)] public float common;
        [Range(0f, 1f)] public float rare;
        [Range(0f, 1f)] public float epic;
        [Range(0f, 1f)] public float legendary;
    }

    [System.Serializable]
    public struct ArtifactGradeColor
    {
        public Color common;
        public Color rare;
        public Color epic;
        public Color legendary;
    }
}
