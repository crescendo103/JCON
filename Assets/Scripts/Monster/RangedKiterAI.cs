using UnityEngine;

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
