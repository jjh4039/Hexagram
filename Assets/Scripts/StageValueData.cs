using System;
using UnityEngine;

[Serializable]
public struct StageValueData
{
    public float stage1;
    public float stage2;
    public float stage3;

    public float GetValue(int stage)
    {
        switch (stage)
        {
            case 1: return stage1;
            case 2: return stage2;
            case 3: return stage3;
            default:
                Debug.LogWarning($"Invalid stage index: {stage}. Stage1 값을 반환합니다.");
                return stage1;
        }
    }
}