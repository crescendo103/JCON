using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일맵 격자 위에서 한 칸씩 플레이어를 추적하는 AI들의 공통 로직.
/// repathInterval마다 코루틴으로 경로를 새로 계산하고, 매 프레임 그 경로를 따라 다음 칸으로 이동한다.
/// 실제 경로탐색 알고리즘(DFS/다익스트라 등)은 FindPath만 하위 클래스가 정해주면 된다.
/// 경로/타이머 등 몬스터별 상태는 이 애셋이 아니라 몬스터에 붙는 TilePathFollower 컴포넌트가 들고 있다
/// (MonsterAIBehavior는 ScriptableObject라 여러 몬스터가 같은 인스턴스를 공유하기 때문).
/// </summary>
public abstract class TileChaserAI : MonsterAIBehavior
{
    public float attackRange = 0.8f;
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
