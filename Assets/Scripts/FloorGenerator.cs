using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FloorGenerator : MonoBehaviour
{
    public Tilemap targetTilemap;
    
    [Header("Base Floor Settings")]
    public List<TileBase> baseTiles;      // 기본 바닥 타일들
    
    [System.Serializable]
    public struct DecorativeTile
    {
        public string name;
        public TileBase tile;
        [Range(0, 100)] public float chance;
    }

    [Header("Decoration Settings")]
    public List<DecorativeTile> decorations; // 장식 타일들

    [ContextMenu("Generate Over Painted Tiles")]
    public void GenerateOverPainted()
    {
        if (targetTilemap == null || baseTiles == null || baseTiles.Count == 0) return;
        
        targetTilemap.CompressBounds();
        BoundsInt bounds = targetTilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // 타일이 존재하는 칸만 랜덤 타일로 교체
                if (targetTilemap.HasTile(pos))
                {
                    TileBase tileToPlace = baseTiles[Random.Range(0, baseTiles.Count)];

                    foreach (var deco in decorations)
                    {
                        if (Random.Range(0f, 100f) < deco.chance)
                        {
                            tileToPlace = deco.tile;
                            break;
                        }
                    }
                    targetTilemap.SetTile(pos, tileToPlace);
                }
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetTilemap);
#endif
        Debug.Log("기존 타일 영역 수정 완료!");
    }
}