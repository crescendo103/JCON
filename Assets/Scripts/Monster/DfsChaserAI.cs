using System.Collections.Generic;
using UnityEngine;

/// <summary>DFS로 찾은(최단은 아닐 수 있는) 첫 경로를 따라간다. 계산이 가볍지만 돌아가는 길이 될 수 있다.</summary>
[CreateAssetMenu(menuName = "Monster/AI/Tile Chaser (DFS)", fileName = "AI_TileChaserDFS")]
public class DfsChaserAI : TileChaserAI
{
    protected override List<Vector2Int> FindPath(StageMapGenerator mapGenerator, Vector3 from, Vector3 to)
        => mapGenerator.FindPathDFS(from, to);
}
