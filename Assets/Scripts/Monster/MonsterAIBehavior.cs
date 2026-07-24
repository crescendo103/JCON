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

    // 플레이어의 현재 이동 방향(PlayerController.CurrentVelocity)을 바탕으로 aimLeadTime 뒤의
    // 위치를 예측한 뒤, aimError 반경 안에서 무작위 오차를 더해 반환한다(항상 정확히 맞지는 않도록).
    // PlayerController가 없거나 멈춰 있으면 현재 위치를 기준으로 오차만 더한다.
    private Vector3 GetPredictedAimPoint(MonsterController monster)
    {
        Vector3 currentPos = monster.target.position;

        var player = monster.target.GetComponent<PlayerController>();
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
