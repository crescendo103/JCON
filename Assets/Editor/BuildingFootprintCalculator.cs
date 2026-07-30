using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// buildingPrefabs에 붙은 Collider2D를 기준으로 buildingSizes / buildingColliderOffsets를
/// 자동으로 계산해주는 에디터 전용 헬퍼.
///
/// 계산 방식:
/// 1. 프리팹을 원점(0,0,0), 회전 없음 상태로 씬 밖에 임시 Instantiate
/// 2. 자식 포함 모든 Collider2D 중 트리거가 아닌 것들만 "실제로 바닥을 막는 콜라이더"로 취급해 Encapsulate
/// 3. 합쳐진 world bounds를 groundTilemap의 cellSize로 나눠 칸 단위 크기/오프셋 산출
///    - offset = (콜라이더 footprint의 좌하단 world 좌표) / cellSize
///      → instance.transform.position이 (0,0,0)이므로 이 값이 곧 "피벗 → footprint 좌하단" 오프셋과 동일
/// 4. 임시 인스턴스 즉시 삭제
/// </summary>
public static class BuildingFootprintCalculator
{
#if UNITY_EDITOR
    public static void CalculateAndApply(StageMapGenerator generator)
    {
        if (generator.buildingPrefabs == null || generator.buildingPrefabs.Length == 0)
        {
            Debug.LogWarning("[BuildingFootprintCalculator] buildingPrefabs가 비어 있습니다.");
            return;
        }

        Vector2 cellSize = GetCellSize(generator);
        int count = generator.buildingPrefabs.Length;

        var sizes = new Vector2Int[count];
        var offsets = new Vector2Int[count];

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = generator.buildingPrefabs[i];

            if (prefab == null)
            {
                Debug.LogWarning($"[BuildingFootprintCalculator] buildingPrefabs[{i}]가 비어 있어 1x1로 기본 설정합니다.");
                sizes[i] = Vector2Int.one;
                offsets[i] = Vector2Int.zero;
                continue;
            }

            if (TryCalculateFootprint(prefab, cellSize, out Vector2Int size, out Vector2Int offset))
            {
                sizes[i] = size;
                offsets[i] = offset;
                Debug.Log($"[BuildingFootprintCalculator] '{prefab.name}' → size={size}, offset={offset}");
            }
            else
            {
                Debug.LogWarning($"[BuildingFootprintCalculator] '{prefab.name}'에서 유효한 (트리거가 아닌) Collider2D를 찾지 못해 1x1로 기본 설정합니다.");
                sizes[i] = Vector2Int.one;
                offsets[i] = Vector2Int.zero;
            }
        }

        Undo.RecordObject(generator, "Auto Calculate Building Footprints");
        generator.buildingSizes = sizes;
        generator.buildingColliderOffsets = offsets;
        EditorUtility.SetDirty(generator);
    }

    private static Vector2 GetCellSize(StageMapGenerator generator)
    {
        if (generator.groundTilemap != null)
        {
            Vector3 cs = generator.groundTilemap.cellSize;
            if (cs.x > 0f && cs.y > 0f) return new Vector2(cs.x, cs.y);
        }

        Debug.LogWarning("[BuildingFootprintCalculator] groundTilemap이 없거나 cellSize가 0이라 1x1 셀 기준으로 계산합니다.");
        return Vector2.one;
    }

    private static bool TryCalculateFootprint(GameObject prefab, Vector2 cellSize, out Vector2Int size, out Vector2Int offset)
    {
        size = Vector2Int.zero;
        offset = Vector2Int.zero;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        try
        {
            // 트리거 콜라이더는 "실제로 바닥을 막는" 영역이 아니므로 제외
            Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>()
                .Where(c => !c.isTrigger)
                .ToArray();

            if (colliders.Length == 0) return false;

            Bounds combined = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                combined.Encapsulate(colliders[i].bounds);

            // instance가 원점에 회전 없이 있으므로 combined.min은 곧 "피벗 기준" 좌하단 좌표와 같음
            Vector2 min = combined.min;
            Vector2 worldSize = combined.size;

            size = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(worldSize.x / cellSize.x)),
                Mathf.Max(1, Mathf.RoundToInt(worldSize.y / cellSize.y))
            );

            offset = new Vector2Int(
                Mathf.RoundToInt(min.x / cellSize.x),
                Mathf.RoundToInt(min.y / cellSize.y)
            );

            return true;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
#endif
}
