using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Grid))]
public class InfiniteTilemapManager : MonoBehaviour
{
    public Transform cameraTransform; // 기준 카메라 트랜스폼
    public TileBase[] randomTiles; // 랜덤 배치할 타일 배열
    public int chunkSize = 20; // 한 구역의 타일 갯수

    [Header("Visual Settings")] public Color tileColor = Color.white; // 타일맵 전체 색상
    public string sortingLayerName = "Default"; // 렌더링 레이어 이름
    public int orderInLayer = 0; // 렌더링 순서

    private GameObject[] chunks; // 9개 구역을 담을 배열
    private Vector2Int currentGrid; // 현재 카메라가 위치한 그리드
    private Color _lastColor; // 이전 색상 비교용
    private int _lastOrder; // 이전 렌더링 순서 비교용

    private Grid _grid; // 캐싱된 그리드 컴포넌트
    private float _worldChunkSize; // 캐싱된 월드 기준 청크 크기

    private void Start()
    {
        _grid = GetComponent<Grid>();
        _worldChunkSize = chunkSize * _grid.cellSize.x;

        InitChunks();
        currentGrid = new Vector2Int(9999, 9999);

        _lastColor = tileColor;
        _lastOrder = orderInLayer;
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        int gridX = Mathf.FloorToInt((cameraTransform.position.x + _worldChunkSize * 0.5f) / _worldChunkSize);
        int gridY = Mathf.FloorToInt((cameraTransform.position.y + _worldChunkSize * 0.5f) / _worldChunkSize);
        Vector2Int newGrid = new Vector2Int(gridX, gridY);

        if (currentGrid != newGrid)
        {
            currentGrid = newGrid;
            UpdateChunksPosition();
        }

        if (tileColor != _lastColor || orderInLayer != _lastOrder)
        {
            ApplyVisualSettings();
            _lastColor = tileColor;
            _lastOrder = orderInLayer;
        }
    }

    private void InitChunks()
    {
        chunks = new GameObject[9];
        for (int i = 0; i < 9; i++)
        {
            chunks[i] = new GameObject("Chunk_" + i);
            chunks[i].transform.SetParent(transform);
            chunks[i].transform.localPosition = Vector3.zero;

            Tilemap tm = chunks[i].AddComponent<Tilemap>();
            TilemapRenderer tr = chunks[i].AddComponent<TilemapRenderer>();

            tr.sortingLayerName = sortingLayerName;
            tr.sortingOrder = orderInLayer;
            tm.color = tileColor;

            FillRandomTiles(tm);
        }
    }

    private void ApplyVisualSettings()
    {
        if (chunks == null) return;

        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i] == null) continue;

            Tilemap tm = chunks[i].GetComponent<Tilemap>();
            TilemapRenderer tr = chunks[i].GetComponent<TilemapRenderer>();

            if (tm != null) tm.color = tileColor;
            if (tr != null) tr.sortingOrder = orderInLayer;
        }
    }

    private void FillRandomTiles(Tilemap tm)
    {
        if (randomTiles == null || randomTiles.Length == 0) return;

        int half = chunkSize / 2;
        for (int x = -half; x < half; x++)
        {
            for (int y = -half; y < half; y++)
            {
                TileBase tile = randomTiles[Random.Range(0, randomTiles.Length)];
                tm.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private void UpdateChunksPosition()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int targetX = currentGrid.x + x;
                int targetY = currentGrid.y + y;

                int wrapX = (targetX % 3 + 3) % 3;
                int wrapY = (targetY % 3 + 3) % 3;
                int index = wrapX * 3 + wrapY;

                float posX = targetX * _worldChunkSize;
                float posY = targetY * _worldChunkSize;

                chunks[index].transform.position = new Vector3(posX, posY, 0);
            }
        }
    }
}