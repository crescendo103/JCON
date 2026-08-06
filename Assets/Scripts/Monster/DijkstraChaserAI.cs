using System.Collections.Generic;
using UnityEngine;

/// <summary>다익스트라로 항상 최단 경로를 계산해서 따라간다.</summary>
[CreateAssetMenu(menuName = "Monster/AI/Tile Chaser (Dijkstra)", fileName = "AI_TileChaserDijkstra")]
public class DijkstraChaserAI : TileChaserAI
{
    protected override List<Vector2Int> FindPath(StageMapGenerator mapGenerator, Vector3 from, Vector3 to)
        => mapGenerator.FindPathDijkstra(from, to);
}
