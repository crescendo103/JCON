using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 4단계 파이프라인 절차적 맵 생성기
/// 1. CA로 벽/바닥 큰 틀 생성
/// 2. 벽 칸에 1x1 장애물 타일 채우기
/// 3. 빈 바닥에 건물 블루프린트(프리팹) 배치
/// 4. 남은 빈 바닥에 장식(나무/덤불) 흩뿌리기
/// </summary>
public class StageMapGenerator : MonoBehaviour
{
    [Header("맵 크기")]
    public int width = 60;
    public int height = 60;

    [Header("CA 설정")]
    [Range(0f, 1f)] public float initialWallDensity = 0.45f;
    public int caIterations = 5;
    public int wallThreshold = 4;

    [Header("Tilemap 참조")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap;
    public TileBase[] groundTiles; // 바닥 타일 (여러 종류면 랜덤 선택)
    public TileBase wallTile; // Rule Tile 권장: 주변 벽 패턴에 맞춰 이음새(코너/직선/T자 등) 자동 선택

    [Header("바깥 여백 (카메라가 맵 밖 빈 공간을 보지 않도록 물 타일로 채움)")]
    public TileBase[] waterTiles; // 물 타일 (여러 종류면 칸마다 랜덤 선택)
    [Tooltip("맵 경계 바깥으로 물 타일을 몇 칸까지 깔지. 카메라가 맵 가장자리까지 붙어도 화면 밖으로 나가지 않을 만큼 넉넉하게")]
    public int waterPadding = 30;

    [Header("건물 블루프린트")]
    public GameObject[] buildingPrefabs; // 각 프리팹은 자체 크기 정보를 buildingSizes와 짝지어 관리
    // 주의: buildingSizes는 "스프라이트 전체 크기"가 아니라 "실제 Collider2D가 막는 바닥 영역(footprint) 크기"임.
    // 지붕처럼 시각적으로 튀어나온 부분은 이 크기에 포함하지 않음 -> occupied 예약도 그만큼만 되고,
    // 다른 오브젝트가 지붕 아래/뒤로 시각적으로 겹쳐도 배치가 허용됨.
    public Vector2Int[] buildingSizes;   // buildingPrefabs와 인덱스 매칭 (콜라이더 기준 가로, 세로 칸 수)
    public Vector2Int[] buildingColliderOffsets; // 프리팹 피벗 기준, 콜라이더 footprint의 좌하단이 피벗으로부터 얼마나 떨어져 있는지 (칸 단위)
    public int buildingPlacementAttempts = 40; // 블루프린트당 자리 찾기 시도 횟수

    [Header("건물 배치 개수 (buildingPrefabs 중 일부만 랜덤 선택)")]
    public int minBuildingsToPlace = 3;
    public int maxBuildingsToPlace = 6; // buildingPrefabs.Length보다 크면 자동으로 clamp됨

    [Header("장식")]
    public GameObject[] decorationPrefabs;
    [Range(0f, 1f)] public float decorationDensity = 0.35f; // 노이즈 임계값
    public float decorationNoiseScale = 0.08f;

    [Header("시드")]
    public int seed = 1; // 스테이지 번호 = 시드값

    // 내부 상태
    private int[,] wallMap;      // 0 = 바닥, 1 = 벽
    private bool[,] occupied;    // 건물이 차지한 칸
    private System.Random rng;
    private Transform contentParent;

    public void GenerateStage(int stageSeed)
    {
        seed = stageSeed;
        rng = new System.Random(seed);
        UnityEngine.Random.InitState(seed); // Rule Tile의 Random 매칭 옵션이 UnityEngine.Random을 쓰므로 같이 시드 고정

        ClearPrevious();

        wallMap = RunCellularAutomata();
        occupied = new bool[width, height];

        DrawGroundAndWalls();
        DrawWaterBorder();
        PlaceBuildings();
        ScatterDecorations();
    }

    // 인스펙터에서 맵 생성 없이 바로 비우고 싶을 때(테스트 중 잘못 쌓인 결과물 정리 등) 쓰는 버튼.
    // contentParent 캐시가 도메인 리로드 등으로 어긋나 있을 수 있으니, 캐시 대신 이름으로 자식을
    // 전부 찾아서 지운다 - "Generate Now"를 에디터에서 여러 번 눌러 GeneratedContent가 중복으로
    // 쌓인 경우까지 이 버튼 한 번으로 정리할 수 있다.
    [ContextMenu("Clear Tilemaps")]
    public void ClearTilemaps()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "GeneratedContent") continue;

            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        contentParent = null;
    }

    // ---------- 0. 이전 결과 정리 ----------
    private void ClearPrevious()
    {
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        // Destroy()는 플레이 모드에서만 실제로 파괴되고, 에디터에서 "Generate Now"로 테스트할 때는
        // 다음 프레임까지 미뤄지는데 에디트 모드는 그 프레임이 안 돌아서 사실상 무시된다.
        // 그 결과 이전 GeneratedContent가 안 지워지고 계속 쌓여서 건물이 중복 생성된 것처럼 보였다.
        if (contentParent != null)
        {
            if (Application.isPlaying) Destroy(contentParent.gameObject);
            else DestroyImmediate(contentParent.gameObject);
        }
        contentParent = new GameObject("GeneratedContent").transform;
        contentParent.SetParent(transform);
    }

    // ---------- 1단계: CA로 벽/바닥 큰 틀 ----------
    private int[,] RunCellularAutomata()
    {
        int[,] map = new int[width, height];

        // 랜덤 초기화 + 테두리 강제 벽
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (IsBorder(x, y))
                    map[x, y] = 1;
                else
                    map[x, y] = rng.NextDouble() < initialWallDensity ? 1 : 0;
            }

        // 스무딩 반복
        for (int i = 0; i < caIterations; i++)
            map = SmoothStep(map);

        return map;
    }

    private bool IsBorder(int x, int y) => x == 0 || y == 0 || x == width - 1 || y == height - 1;

    private int[,] SmoothStep(int[,] src)
    {
        int[,] next = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (IsBorder(x, y)) { next[x, y] = 1; continue; }

                int wallCount = CountWallNeighbors(src, x, y);
                if (wallCount > wallThreshold) next[x, y] = 1;
                else if (wallCount < wallThreshold) next[x, y] = 0;
                else next[x, y] = src[x, y];
            }
        return next;
    }

    private int CountWallNeighbors(int[,] src, int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) { count++; continue; }
                count += src[nx, ny];
            }
        return count;
    }

    // ---------- 2단계: 벽 칸에 1x1 장애물 타일, 나머지는 바닥 타일 ----------
    private void DrawGroundAndWalls()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase ground = groundTiles[rng.Next(groundTiles.Length)];
                groundTilemap.SetTile(pos, ground); // 바닥은 항상 깔아둠 (벽 밑에도 상관없음)

                if (wallMap[x, y] == 1)
                {
                    // Rule Tile이면 SetTile 시점에 주변 벽 패턴을 보고 알맞은 스프라이트(코너/직선/T자 등)를
                    // 자동으로 골라줌 -> 여기서 랜덤 선택할 필요 없음
                    wallTilemap.SetTile(pos, wallTile);
                }
            }

        // 벽 Tilemap에 Composite Collider가 붙어 있다는 전제 (에디터에서 미리 세팅)
    }

    // ---------- 맵 바깥 여백을 물 타일로 채우기 (카메라가 맵 경계까지 붙어도 빈 공간이 안 보이게) ----------
    private void DrawWaterBorder()
    {
        if (waterTiles == null || waterTiles.Length == 0) return;

        int minX = -waterPadding;
        int maxX = width + waterPadding;
        int minY = -waterPadding;
        int maxY = height + waterPadding;

        for (int x = minX; x < maxX; x++)
            for (int y = minY; y < maxY; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height) continue; // 맵 내부는 그대로 둠
                TileBase water = waterTiles[rng.Next(waterTiles.Length)]; // 칸마다 랜덤 선택 (위치는 고정, 어떤 타일을 쓸지만 시드로 랜덤)
                groundTilemap.SetTile(new Vector3Int(x, y, 0), water);
            }
    }

    // buildingPrefabs 전체 중 이번 스테이지에 실제로 배치할 개수만큼 인덱스를 랜덤 선택.
    // 반드시 this.rng(=seed로 초기화된 인스턴스)만 사용해서 셔플해야 동일 시드 -> 동일 결과가 유지됨.
    private List<int> SelectBuildingIndices()
    {
        int total = Mathf.Min(buildingPrefabs.Length, buildingSizes.Length);
        if (buildingPrefabs.Length != buildingSizes.Length)
            Debug.LogWarning($"StageMapGenerator: buildingPrefabs({buildingPrefabs.Length}개)와 buildingSizes({buildingSizes.Length}개) 배열 길이가 다릅니다. " +
                              $"인스펙터에서 두 배열을 같은 길이로 맞춰주세요. 우선 앞쪽 {total}개만 사용합니다.");
        if (total == 0) return new List<int>();

        int min = Mathf.Max(0, minBuildingsToPlace);
        int max = Mathf.Max(min, maxBuildingsToPlace);

        // rng.Next(min, max)는 max를 포함하지 않으므로 +1
        int count = rng.Next(min, max + 1);

        // 매번 독립적으로 추첨(장식물과 동일한 방식) -> 같은 건물이 여러 번 나올 수 있음.
        // 셔플 없이 뽑으므로 total(프리팹 종류 수)보다 많은 개수도 배치 가능.
        var indices = new List<int>(count);
        for (int i = 0; i < count; i++)
            indices.Add(rng.Next(total));

        return indices;
    }

    // ---------- 3단계: 빈 바닥에 건물 블루프린트 배치 ----------
    private void PlaceBuildings()
    {
        // 이번 스테이지에 실제로 배치할 프리팹 인덱스를 랜덤으로 일부만 선택
        // (반드시 seed 기반 rng를 사용해야 동일 시드 -> 동일 맵이 유지됨. UnityEngine.Random 절대 쓰지 말 것)
        List<int> selectedIndices = SelectBuildingIndices();

        // 선택된 것들 중에서도 큰 건물부터 배치 (자리 선점 우선순위)
        var order = selectedIndices
            .OrderByDescending(i => buildingSizes[i].x * buildingSizes[i].y)
            .ToList();

        foreach (int i in order)
        {
            GameObject prefab = buildingPrefabs[i];
            Vector2Int size = buildingSizes[i];

            for (int attempt = 0; attempt < buildingPlacementAttempts; attempt++)
            {
                int x = rng.Next(1, width - size.x - 1);
                int y = rng.Next(1, height - size.y - 1);

                if (CanPlaceBuilding(x, y, size))
                {
                    InstantiateBuilding(x, y, prefab, i);
                    MarkOccupied(x, y, size);
                    break; // 이 블루프린트는 배치 성공, 다음 블루프린트로
                }
            }
            // 시도 횟수 다 써도 못 찾으면 그냥 스킵 (맵이 너무 빽빽한 경우 방어)
        }
    }

    private bool CanPlaceBuilding(int x, int y, Vector2Int size)
    {
        if (x + size.x >= width || y + size.y >= height) return false;

        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (wallMap[nx, ny] == 1) return false;  // 벽 위에는 못 놓음
                if (occupied[nx, ny]) return false;       // 다른 건물과 겹침
            }
        return true;
    }

    private void MarkOccupied(int x, int y, Vector2Int size)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                occupied[x + dx, y + dy] = true;
    }

    private void InstantiateBuilding(int x, int y, GameObject prefab, int prefabIndex)
    {
        // x, y는 "콜라이더 footprint의 좌하단 칸" 기준. 프리팹 피벗이 스프라이트 중앙/하단 등
        // 다른 곳에 있을 수 있으므로, 등록해둔 offset만큼 빼서 실제 Instantiate 위치를 역산.
        Vector2Int offset = (buildingColliderOffsets != null && buildingColliderOffsets.Length > prefabIndex)
            ? buildingColliderOffsets[prefabIndex]
            : Vector2Int.zero;

        Vector3Int pivotCell = new Vector3Int(x - offset.x, y - offset.y, 0);
        Vector3 worldPos = groundTilemap.CellToWorld(pivotCell);
        Instantiate(prefab, worldPos, Quaternion.identity, contentParent);
    }

    // ---------- 4단계: 남은 빈 바닥에 장식 흩뿌리기 ----------
    private void ScatterDecorations()
    {
        if (decorationPrefabs.Length == 0) return;

        float offsetX = seed * 1000f;
        float offsetY = seed * 1000f;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (wallMap[x, y] == 1 || occupied[x, y]) continue; // 벽/건물 위에는 안 놓음

                float noise = Mathf.PerlinNoise((x + offsetX) * decorationNoiseScale,
                                                 (y + offsetY) * decorationNoiseScale);
                if (noise < decorationDensity) continue;

                // 밀도 통과했으면 확률적으로 실제 배치 (너무 빽빽하지 않게)
                if (rng.NextDouble() >= 0.15) continue;

                GameObject prefab = decorationPrefabs[rng.Next(decorationPrefabs.Length)];
                if (prefab == null) continue;

                // 장식물 프리팹이 1칸보다 큰 경우(사람 무리, 덤불 등) 실제 스프라이트가 차지하는
                // 칸 전체가 벽/건물/다른 장식물과 겹치지 않을 때만 배치한다 (건물 배치와 동일한 방식).
                GetTilemapFootprint(prefab, out Vector2Int footprintOrigin, out Vector2Int footprintSize);
                int originX = x + footprintOrigin.x;
                int originY = y + footprintOrigin.y;
                if (!CanPlaceFootprint(originX, originY, footprintSize)) continue;

                Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(x, y, 0));
                Instantiate(prefab, worldPos, Quaternion.identity, contentParent);
                MarkOccupied(originX, originY, footprintSize);
            }
    }

    // 장식물 프리팹이 실제로 차지하는 타일 칸 범위를 프리팹 자신의 Tilemap 데이터에서 읽어온다.
    // (건물처럼 별도 크기 배열을 관리할 필요 없이, 타일맵에 이미 있는 정보를 그대로 사용)
    private void GetTilemapFootprint(GameObject prefab, out Vector2Int origin, out Vector2Int size)
    {
        Tilemap prefabTilemap = prefab.GetComponent<Tilemap>();
        if (prefabTilemap == null)
        {
            origin = Vector2Int.zero;
            size = Vector2Int.one;
            return;
        }

        BoundsInt bounds = prefabTilemap.cellBounds;
        origin = new Vector2Int(bounds.xMin, bounds.yMin);
        size = new Vector2Int(Mathf.Max(1, bounds.size.x), Mathf.Max(1, bounds.size.y));
    }

    // CanPlaceBuilding과 같은 방식(벽/건물/다른 배치물과 겹치지 않는지)으로 임의의 칸 범위를 검사한다.
    private bool CanPlaceFootprint(int x, int y, Vector2Int size)
    {
        if (x < 0 || y < 0 || x + size.x > width || y + size.y > height) return false;

        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                if (wallMap[x + dx, y + dy] == 1) return false;
                if (occupied[x + dx, y + dy]) return false;
            }
        return true;
    }
}