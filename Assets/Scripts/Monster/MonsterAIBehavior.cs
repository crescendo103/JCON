using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 AI 패턴의 부모 클래스.
/// 이 클래스를 상속받는 새 클래스를 추가하면 자동으로 Monster Maker의
/// "AI 패턴" 드롭다운에 나타납니다 (리플렉션으로 자동 탐색).
/// </summary>
public abstract class MonsterAIBehavior : ScriptableObject
{
    public abstract void Execute(MonsterController monster);
}

[CreateAssetMenu(menuName = "Monster/AI/Aggressive", fileName = "AI_Aggressive")]
public class AggressiveAI : MonsterAIBehavior
{
    public float attackRange = 1.2f;

    public override void Execute(MonsterController monster)
    {
        if (monster.target == null) return;

        float dist = Vector2.Distance(monster.transform.position, monster.target.position);

        if (dist > attackRange)
        {
            // 플레이어에게 돌진
            monster.MoveTowards(monster.target.position);
        }
        else
        {
            // 공격 사거리 안 → 공격 (쿨타임이 지났으면 스킬, 아니면 일반 공격)
            monster.Stop();
            monster.AttackTarget();
            Debug.Log($"{monster.data?.monsterName}이 공격!");
        }
    }
}

[CreateAssetMenu(menuName = "Monster/AI/Ranged Kiter", fileName = "AI_RangedKiter")]
public class RangedKiterAI : MonsterAIBehavior
{
    public float preferredDistance = 6f;
    public float tolerance = 1f;

    // 플레이어가 이동 중일 때 이 시간(초)만큼 앞선 위치를 예측해서 조준한다(리드샷).
    public float aimLeadTime = 0.5f;

    // 예측 조준 지점에 더해지는 무작위 오차의 최대 반경(원 안에서 균등 분포). 0이면 오차 없이 정확히 조준.
    public float aimError = 1f;

    public override void Execute(MonsterController monster)
    {
        if (monster.target == null) return;

        float dist = Vector2.Distance(monster.transform.position, monster.target.position);

        if (dist < preferredDistance - tolerance)
        {
            // 너무 가까움 → 뒤로 물러남
            Vector3 away = monster.transform.position - monster.target.position;
            Vector3 retreatPos = monster.transform.position + away.normalized;
            monster.MoveTowards(retreatPos);
        }
        else if (dist > preferredDistance + tolerance)
        {
            // 너무 멀음 → 다가감
            monster.MoveTowards(monster.target.position);
        }
        else
        {
            // 적정 거리 → 원거리 공격 (쿨타임이 지났으면 스킬, 아니면 일반 공격)
            monster.Stop();
            monster.AttackTarget(GetPredictedAimPoint(monster));
            Debug.Log($"{monster.data?.monsterName}이 원거리 공격!");
        }
    }

    // 플레이어의 현재 이동 방향(GamePlayerController.CurrentVelocity)을 바탕으로 aimLeadTime 뒤의
    // 위치를 예측한 뒤, aimError 반경 안에서 무작위 오차를 더해 반환한다(항상 정확히 맞지는 않도록).
    // GamePlayerController가 없거나 멈춰 있으면 현재 위치를 기준으로 오차만 더한다.
    private Vector3 GetPredictedAimPoint(MonsterController monster)
    {
        Vector3 currentPos = monster.target.position;

        var player = monster.target.GetComponent<GamePlayerController>();
        Vector3 predicted = player != null
            ? currentPos + (Vector3)(player.CurrentVelocity * aimLeadTime)
            : currentPos;

        if (aimError > 0f)
        {
            predicted += (Vector3)(Random.insideUnitCircle * aimError);
        }

        return predicted;
    }
}

/// <summary>
/// 타일맵 격자 위에서 한 칸씩 플레이어를 추적하는 AI들의 공통 로직.
/// repathInterval마다 코루틴으로 경로를 새로 계산하고, 매 프레임 그 경로를 따라 다음 칸으로 이동한다.
/// 실제 경로탐색 알고리즘(DFS/다익스트라 등)은 FindPath만 하위 클래스가 정해주면 된다.
/// 경로/타이머 등 몬스터별 상태는 이 애셋이 아니라 몬스터에 붙는 TilePathFollower 컴포넌트가 들고 있다
/// (MonsterAIBehavior는 ScriptableObject라 여러 몬스터가 같은 인스턴스를 공유하기 때문).
/// </summary>
public abstract class TileChaserAI : MonsterAIBehavior
{
    public float attackRange = 1.2f;
    [Tooltip("경로를 다시 계산하는 주기(초)")]
    public float repathInterval = 1f;

    protected abstract List<Vector2Int> FindPath(StageMapGenerator mapGenerator, Vector3 from, Vector3 to);

    public override void Execute(MonsterController monster)
    {
        if (monster.target == null) return;

        float dist = Vector2.Distance(monster.transform.position, monster.target.position);
        if (dist <= attackRange)
        {
            monster.Stop();
            monster.AttackTarget();
            return;
        }

        TilePathFollower follower = monster.GetComponent<TilePathFollower>();
        if (follower == null) follower = monster.gameObject.AddComponent<TilePathFollower>();

        if (follower.MapGenerator == null)
        {
            monster.MoveTowards(monster.target.position); // 맵 생성기가 없는 씬이면 기존 직선 추적으로 대체
            return;
        }

        if (!follower.RepathRoutineStarted)
        {
            follower.MarkRepathRoutineStarted();
            monster.StartCoroutine(RepathRoutine(monster, follower));
        }

        Vector3? nextStep = follower.GetNextStepWorldPos(monster.transform.position);
        if (nextStep.HasValue)
            monster.MoveTowards(nextStep.Value);
        else
            monster.Stop();
    }

    // repathInterval마다 FindPath로 경로를 새로 계산해서 follower에 반영한다.
    // 몬스터가 파괴되거나 타겟을 잃으면 코루틴이 자연히 끝난다(MonsterController.Die()가 StopAllCoroutines도 호출함).
    private IEnumerator RepathRoutine(MonsterController monster, TilePathFollower follower)
    {
        while (monster != null && monster.target != null)
        {
            List<Vector2Int> path = FindPath(follower.MapGenerator, monster.transform.position, monster.target.position);
            follower.SetPath(path);
            yield return new WaitForSeconds(repathInterval);
        }
    }
}

/// <summary>DFS로 찾은(최단은 아닐 수 있는) 첫 경로를 따라간다. 계산이 가볍지만 돌아가는 길이 될 수 있다.</summary>
[CreateAssetMenu(menuName = "Monster/AI/Tile Chaser (DFS)", fileName = "AI_TileChaserDFS")]
public class DfsChaserAI : TileChaserAI
{
    protected override List<Vector2Int> FindPath(StageMapGenerator mapGenerator, Vector3 from, Vector3 to)
        => mapGenerator.FindPathDFS(from, to);
}

/// <summary>다익스트라로 항상 최단 경로를 계산해서 따라간다.</summary>
[CreateAssetMenu(menuName = "Monster/AI/Tile Chaser (Dijkstra)", fileName = "AI_TileChaserDijkstra")]
public class DijkstraChaserAI : TileChaserAI
{
    protected override List<Vector2Int> FindPath(StageMapGenerator mapGenerator, Vector3 from, Vector3 to)
        => mapGenerator.FindPathDijkstra(from, to);
}

[CreateAssetMenu(menuName = "Monster/AI/Passive", fileName = "AI_Passive")]
public class PassiveAI : MonsterAIBehavior
{
    public override void Execute(MonsterController monster)
    {
        // 아무 행동도 하지 않음 (공격받기 전까지 가만히)
        // 공격받았을 때 반응시키려면 MonsterController.TakeDamage() 쪽에서
        // aiBehavior를 다른 AI로 교체하는 방식을 추천
    }
}
