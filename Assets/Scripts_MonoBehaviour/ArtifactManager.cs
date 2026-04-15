using UnityEngine;
using System.Collections.Generic;

// 플레이어의 아티팩트 데이터를 관리하는 매니저
public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager instance; // 전역 접근용 인스턴스

    public List<ArtifactData> myArtifacts = new List<ArtifactData>(); // 플레이어가 획득한 아티팩트 목록

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // 외부에서 새로운 아티팩트를 획득했을 때 호출하는 함수
    public void AddArtifact(ArtifactData data)
    {
        myArtifacts.Add(data); // 목록에 데이터 추가

        // PlayerStats.Apply(data); // 추후 스탯 적용 로직 연결 부분

        Debug.Log($"아티팩트 획득: {data.artifactName}");

        if (DashboardUI.instance && DashboardUI.instance.isOpen)
        {
            DashboardUI.instance.RefreshArtifacts(); // 인벤토리가 열려있을 경우 즉시 UI 갱신
        }
    }
}