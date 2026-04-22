using UnityEngine;
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager instance;                     // 전역 접근용 인스턴스

    public List<ArtifactData> myArtifacts = new List<ArtifactData>(); 

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AddArtifact(ArtifactData data)
    {
        myArtifacts.Add(data);                                  // 인벤토리 목록에 추가

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
            DashboardUI.instance.RefreshArtifacts();            // 열려있는 UI 즉시 갱신
        }
    }
}