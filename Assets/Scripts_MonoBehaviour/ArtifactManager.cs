using UnityEngine;
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager instance; // 전역 접근용 인스턴스

    public List<ArtifactData> myArtifacts = new List<ArtifactData>();

    // ★ 신규 추가: 전체 아티팩트 데이터베이스 (인스펙터에서 BitManager와 동일하게 할당)
    public List<ArtifactData> allArtifacts = new List<ArtifactData>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AddArtifact(ArtifactData data)
    {
        if (data == null) return;

        myArtifacts.Add(data); // 인벤토리 목록에 추가

        // 획득한 아티팩트가 영구 스탯형이라면 플레이어 스탯에 즉시 반영
        if (data.type == ArtifactType.Stat)
        {
            if (GameManager.instance && GameManager.instance.stats)
            {
                GameManager.instance.stats.ApplyArtifactStat(data);
            }
        }

        Debug.Log($"아티팩트 획득: {data.artifactName}");

        if (DashboardUI.instance && DashboardUI.instance.isOpen)
        {
            DashboardUI.instance.RefreshArtifacts(); // 열려있는 UI 즉시 갱신
        }
    }

    // ★ 신규 추가: 등급에 맞는 무작위 아티팩트를 플레이어에게 즉시 지급
    public void GiveRandomArtifactByGrade(ArtifactGrade grade)
    {
        if (allArtifacts == null || allArtifacts.Count == 0)
        {
            Debug.LogError("ArtifactManager: allArtifacts 리스트가 비어있습니다!");
            return;
        }

        List<ArtifactData> candidates = new List<ArtifactData>();

        foreach (var artifact in allArtifacts)
        {
            if (artifact.grade == grade && !myArtifacts.Contains(artifact))
            {
                candidates.Add(artifact);
            }
        }

        // 해당 등급의 아티팩트를 모두 가졌다면 아무거나(가장 낮은 등급부터) 지급
        if (candidates.Count == 0)
        {
            foreach (var artifact in allArtifacts)
            {
                if (!myArtifacts.Contains(artifact)) candidates.Add(artifact);
            }
        }

        if (candidates.Count > 0)
        {
            ArtifactData selected = candidates[Random.Range(0, candidates.Count)];
            AddArtifact(selected);  
        }
    }

    public void OnStageEnterTrigger()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return;
        BuffManager buffManager = GameManager.instance.player.GetComponent<BuffManager>();
        if (buffManager == null) return;

        foreach (var artifact in myArtifacts)
        {
            if (artifact.type == ArtifactType.Trigger && artifact.condition == ConditionType.OnStageEnter)
            {
                buffManager.ApplyArtifactBuff(artifact);
                Debug.Log($"스테이지 진입 트리거 발동: {artifact.artifactName}");
            }
        }
    }
}