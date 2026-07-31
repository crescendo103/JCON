using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일 기반 추적 AI(DfsChaserAI, DijkstraChaserAI 등)가 사용하는 몬스터별 경로 상태 저장용 컴포넌트.
/// MonsterAIBehavior는 ScriptableObject라 여러 몬스터가 같은 AI 애셋 인스턴스를 공유하므로,
/// 현재 경로/진행 인덱스/재계산 코루틴 시작 여부 같은 몬스터별 상태는 AI 쪽이 아니라
/// 몬스터 GameObject에 붙는 이 컴포넌트에 보관한다.
/// </summary>
public class TilePathFollower : MonoBehaviour
{
    private StageMapGenerator mapGenerator;
    private List<Vector2Int> path;
    private int pathIndex;
    private bool repathRoutineStarted;

    /// <summary>씬에서 찾은 맵 생성기. 없으면 null(타일 경로 추적을 쓸 수 없는 씬).</summary>
    public StageMapGenerator MapGenerator => mapGenerator;

    public bool RepathRoutineStarted => repathRoutineStarted;
    public void MarkRepathRoutineStarted() => repathRoutineStarted = true;

    private void Awake()
    {
        mapGenerator = FindFirstObjectByType<StageMapGenerator>();
    }

    public void SetPath(List<Vector2Int> newPath)
    {
        path = newPath;
        pathIndex = 0;
    }

    private bool HasPath => path != null && pathIndex < path.Count;

    /// <summary>
    /// 현재 경로에서 다음으로 향해야 할 칸의 월드 좌표를 반환한다. 이미 그 칸에 도착했으면 다음 칸으로
    /// 넘어간 뒤의 좌표를 반환하고, 경로 끝까지 도착했거나 경로가 없으면 null을 반환한다.
    /// </summary>
    public Vector3? GetNextStepWorldPos(Vector3 currentWorldPos, float arriveThreshold = 0.15f)
    {
        if (mapGenerator == null || !HasPath) return null;

        Vector3 cellWorld = mapGenerator.GetCellCenterWorld(path[pathIndex].x, path[pathIndex].y);

        if (((Vector2)currentWorldPos - (Vector2)cellWorld).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            pathIndex++;
            if (!HasPath) return null;
            cellWorld = mapGenerator.GetCellCenterWorld(path[pathIndex].x, path[pathIndex].y);
        }

        return cellWorld;
    }
}
