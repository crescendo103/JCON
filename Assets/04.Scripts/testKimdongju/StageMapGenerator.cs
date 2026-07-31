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
    private bool[,] occupied;    // 건물/장식이 차지한 칸 (배치 시 겹침 방지용 - 장식은 콜라이더가 없어 이동은 막지 않음)
    private bool[,] blocksMovement; // 실제로 이동/스폰/경로탐색을 막는 칸 (벽 + 건물만. 장식은 플레이어처럼 지나갈 수 있어 제외)
    private System.Random rng;
    private Transform contentParent;

    // 스폰 가능 여부(도달 가능 범위) 캐시. ComputeReachability가 한 번 계산해두면 재사용한다.
    private bool[,] reachable;
    private bool reachabilityComputed;

    private static readonly Vector2Int[] FourDirections =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    public void GenerateStage(int stageSeed)
    {
        seed = stageSeed;
        rng = new System.Random(seed);
        UnityEngine.Random.InitState(seed); // Rule Tile의 Random 매칭 옵션이 UnityEngine.Random을 쓰므로 같이 시드 고정

        ClearPrevious();

        wallMap = RunCellularAutomata();
        occupied = new bool[width, height];
        blocksMovement = new bool[width, height];
        reachable = null;
        reachabilityComputed = false;

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

    // blocksMovementToo: 건물처럼 실제 콜라이더가 있어서 이동도 막는 오브젝트면 true, 장식처럼 콜라이더
    // 없이 시각적으로만 배치되는 오브젝트면 false (겹침 방지용 occupied만 표시하고 이동은 막지 않음).
    private void MarkOccupied(int x, int y, Vector2Int size, bool blocksMovementToo = true)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                occupied[x + dx, y + dy] = true;
                if (blocksMovementToo) blocksMovement[x + dx, y + dy] = true;
            }
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
                MarkOccupied(originX, originY, footprintSize, blocksMovementToo: false); // 장식은 콜라이더가 없어 이동을 막지 않음
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

    // ---------- 스폰 가능 여부 조회 (StageManager 등 외부에서 사용) ----------

    public int Width => width;
    public int Height => height;

    /// <summary>맵 칸 좌표(x, y)의 월드 중심 좌표. 스폰 위치 후보를 뽑을 때 사용.</summary>
    public Vector3 GetCellCenterWorld(int x, int y) => groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));

    /// <summary>
    /// 맵에서 가장 큰 연결된 걸을 수 있는 구역 안의 칸 하나를 무작위로 골라 월드 중심 좌표를 반환한다.
    /// 걸을 수 있는 칸이라도 벽으로 둘러싸여 나머지 맵과 끊긴 작은 고립 구역에는 플레이어가 시작하지
    /// 않도록, 단순히 걸을 수 있는 칸 전체가 아니라 4방향으로 연결된 구역 중 제일 큰 것만 후보로 삼는다.
    /// 플레이어 시작 위치처럼 스테이지 생성 직후 한 번만 뽑는 용도. seed 기반 rng를 사용하므로
    /// 같은 시드 -> 같은 시작 위치가 유지된다. 걸을 수 있는 칸이 하나도 없는 극단적인 경우엔 맵 중앙을 반환.
    /// GenerateStage 이후(벽/건물/장식이 모두 배치된 뒤)에 호출해야 한다.
    /// </summary>
    public Vector3 GetRandomWalkableWorldPosition()
    {
        List<Vector2Int> largestRegion = FindLargestWalkableRegion();
        if (largestRegion.Count == 0) return GetCellCenterWorld(width / 2, height / 2);

        Vector2Int cell = largestRegion[rng.Next(largestRegion.Count)];
        return GetCellCenterWorld(cell.x, cell.y);
    }

    // 맵 전체를 4방향으로 연결된 걸을 수 있는 구역들로 나누고, 그중 칸 수가 가장 많은 구역을 반환한다.
    private List<Vector2Int> FindLargestWalkableRegion()
    {
        var visited = new bool[width, height];
        var largest = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (visited[x, y] || !IsCellWalkable(x, y)) continue;

                List<Vector2Int> region = FloodFillWalkableRegion(x, y, visited);
                if (region.Count > largest.Count) largest = region;
            }

        return largest;
    }

    // (startX, startY)에서 4방향 플러드필로 이어지는 걸을 수 있는 칸을 전부 모아 반환하고, visited에 표시한다.
    private List<Vector2Int> FloodFillWalkableRegion(int startX, int startY, bool[,] visited)
    {
        var region = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(startX, startY);
        visited[startX, startY] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            region.Add(cur);

            foreach (Vector2Int d in FourDirections)
            {
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (!InBounds(nx, ny) || visited[nx, ny] || !IsCellWalkable(nx, ny)) continue;
                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
        return region;
    }

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    /// <summary>
    /// 해당 칸이 벽도 아니고 건물도 차지하지 않아 실제로 지나갈 수 있는지. 장식(나무/덤불 등)은 콜라이더가
    /// 없어 플레이어가 그냥 지나갈 수 있으므로 여기서는 막지 않는다 - 배치 겹침 방지용 occupied와는 별개.
    /// </summary>
    public bool IsCellWalkable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return wallMap[x, y] == 0 && !blocksMovement[x, y];
    }

    /// <summary>
    /// seedWorldPos(보통 플레이어 위치)에서 4방향 플러드필로 실제로 걸어서 갈 수 있는 바닥 칸을 모두 표시한다.
    /// 벽으로 둘러싸여 플레이어가 갈 수 없는 고립된 빈 바닥 칸은 여기서 제외된다.
    /// 이미 계산된 적이 있으면(같은 스테이지에서) 다시 계산하지 않는다 - GenerateStage에서 새 스테이지마다 리셋됨.
    /// </summary>
    public void ComputeReachability(Vector3 seedWorldPos)
    {
        if (reachabilityComputed) return;

        reachable = new bool[width, height];
        Vector3Int seedCell = groundTilemap.WorldToCell(seedWorldPos);
        Vector2Int start = FindNearestWalkableCell(new Vector2Int(seedCell.x, seedCell.y));

        reachabilityComputed = true; // 못 찾아도 계산 자체는 끝난 것으로 처리 (매 프레임 재시도 방지)
        if (start.x < 0) return;     // 맵에 걸어갈 수 있는 칸이 하나도 없는 극단적인 경우 - reachable 전부 false로 남음

        var queue = new Queue<Vector2Int>();
        reachable[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach (Vector2Int d in FourDirections)
            {
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (!InBounds(nx, ny) || reachable[nx, ny] || !IsCellWalkable(nx, ny)) continue;
                reachable[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }

    // start 칸 자체가 벽/건물 위일 수 있으므로(예: 시드 좌표가 약간 어긋난 경우), 인접 칸으로 퍼져나가며
    // 가장 가까운 걸을 수 있는 칸을 찾는다. 벽인 칸도 큐에 넣어 계속 퍼지므로 벽 몇 겹 안쪽에서 시작해도 찾아낸다.
    private Vector2Int FindNearestWalkableCell(Vector2Int start)
    {
        if (InBounds(start.x, start.y) && IsCellWalkable(start.x, start.y))
            return start;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach (Vector2Int d in FourDirections)
            {
                Vector2Int next = cur + d;
                if (!InBounds(next.x, next.y) || visited.Contains(next)) continue;
                visited.Add(next);
                if (IsCellWalkable(next.x, next.y)) return next;
                queue.Enqueue(next);
            }
        }
        return new Vector2Int(-1, -1); // 찾지 못함
    }

    /// <summary>
    /// 이 월드 좌표가 몬스터를 스폰해도 되는 자리인지: 벽/건물 위가 아니고(장식 위는 허용), ComputeReachability가
    /// 계산됐다면 플레이어가 실제로 갈 수 있는 영역인지까지 확인한다.
    /// ComputeReachability를 아직 호출하지 않았다면 벽/건물 회피만 검사한다(호출 전 방어적 동작).
    /// </summary>
    public bool IsWorldPositionSpawnable(Vector3 worldPos)
    {
        Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);
        if (!IsCellWalkable(cellPos.x, cellPos.y)) return false;

        if (!reachabilityComputed) return true;
        return reachable[cellPos.x, cellPos.y];
    }

    // ---------- 타일 경로탐색 (몬스터 AI가 사용) ----------

    /// <summary>월드 좌표가 속한 칸 좌표.</summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    /// <summary>
    /// DFS로 from에서 to까지 걸어갈 수 있는 경로를 찾는다. 최단 경로는 보장하지 않지만, 매번 목표에
    /// 가까워지는 방향을 먼저 시도해서(그리디) 열린 공간에서는 거의 직선으로 다가가고 벽에 막혔을 때만
    /// DFS의 스택 기반 되돌아가기가 발동한다. 순수 무작위 순서로 시도하면 열린 맵에서 첫 경로 자체가
    /// "목표까지의 임의 행보"에 가까워져 아주 길고 구불구불해지고, repathInterval마다 그 경로를 통째로
    /// 버리고 다시 계산하니 실제로는 거의 진전이 없어 보이는 문제가 있었다(플레이어가 가만히 있어도 못 옴).
    /// 그래서 목표까지의 거리가 똑같이 줄어드는 방향이 여럿일 때만(동률일 때만) 그 사이에서 무작위로
    /// 순서를 섞어, 매번 완전히 똑같은 모양으로 도는 문제 없이도 꾸준히 목표 쪽으로 진행하게 한다.
    /// 반환값은 from이 속한 칸부터 to가 속한 칸까지 순서대로 나열한 칸 좌표 목록이고, 도달 불가하면 null.
    /// </summary>
    public List<Vector2Int> FindPathDFS(Vector3 fromWorldPos, Vector3 toWorldPos)
    {
        Vector2Int start = WorldToCell(fromWorldPos);
        Vector2Int goal = WorldToCell(toWorldPos);
        if (!InBounds(goal.x, goal.y) || !IsCellWalkable(goal.x, goal.y)) return null;

        var visited = new HashSet<Vector2Int> { start };
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var stack = new Stack<Vector2Int>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Pop();
            if (cur == goal) return BuildPath(parent, start, goal);

            // 스택은 후입선출이라, 목표에서 먼 방향부터 push해서 가장 가까운 방향이 맨 위(다음 pop)에 오게 한다.
            foreach (Vector2Int d in OrderDirectionsAwayFromGoal(cur, goal))
            {
                Vector2Int next = cur + d;
                if (visited.Contains(next) || !IsCellWalkable(next.x, next.y)) continue;

                visited.Add(next);
                parent[next] = cur;
                stack.Push(next);
            }
        }
        return null; // 도달 불가
    }

    // FourDirections를 goal까지의 맨해튼 거리가 먼 순서대로 정렬해서 반환한다. 거리가 같은 방향들은
    // 먼저 무작위로 섞어둔 뒤 정렬하므로(OrderByDescending은 안정 정렬) 동률 사이에서만 순서가 매번 바뀐다.
    private Vector2Int[] OrderDirectionsAwayFromGoal(Vector2Int from, Vector2Int goal)
    {
        return ShuffleDirections()
            .OrderByDescending(d => ManhattanDistance(from + d, goal))
            .ToArray();
    }

    // FourDirections를 무작위로 섞은 새 배열로 반환한다(원본은 그대로 둠).
    private Vector2Int[] ShuffleDirections()
    {
        var dirs = (Vector2Int[])FourDirections.Clone();
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }
        return dirs;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    /// <summary>
    /// 다익스트라로 from에서 to까지 항상 최단 경로를 찾는다(칸마다 이동 비용이 같은 균일 그리드라
    /// 결과 자체는 BFS와 같지만, 다익스트라 방식(거리값이 가장 작은 칸을 매번 뽑아 확정)으로 구현했다).
    /// 반환/도달 불가 규칙은 FindPathDFS와 동일.
    /// </summary>
    public List<Vector2Int> FindPathDijkstra(Vector3 fromWorldPos, Vector3 toWorldPos)
    {
        Vector2Int start = WorldToCell(fromWorldPos);
        Vector2Int goal = WorldToCell(toWorldPos);
        if (!InBounds(goal.x, goal.y) || !IsCellWalkable(goal.x, goal.y)) return null;

        var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var frontier = new List<Vector2Int> { start };

        while (frontier.Count > 0)
        {
            int bestIndex = 0;
            for (int i = 1; i < frontier.Count; i++)
                if (dist[frontier[i]] < dist[frontier[bestIndex]]) bestIndex = i;

            Vector2Int cur = frontier[bestIndex];
            frontier.RemoveAt(bestIndex);

            if (cur == goal) return BuildPath(parent, start, goal);
            if (!visited.Add(cur)) continue;

            foreach (Vector2Int d in FourDirections)
            {
                Vector2Int next = cur + d;
                if (visited.Contains(next) || !IsCellWalkable(next.x, next.y)) continue;

                int newDist = dist[cur] + 1;
                if (!dist.TryGetValue(next, out int oldDist) || newDist < oldDist)
                {
                    dist[next] = newDist;
                    parent[next] = cur;
                    frontier.Add(next);
                }
            }
        }
        return null; // 도달 불가
    }

    // parent 역추적 맵으로 start부터 goal까지의 경로를 복원한다(start 포함, goal 포함).
    private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int> { goal };
        Vector2Int cur = goal;
        while (cur != start)
        {
            cur = parent[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }
}