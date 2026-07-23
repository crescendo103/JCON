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
            monster.AttackTarget();
            Debug.Log($"{monster.data?.monsterName}이 원거리 공격!");
        }
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

[CreateAssetMenu(menuName = "Monster/AI/Guardian", fileName = "AI_Guardian")]
public class GuardianAI : MonsterAIBehavior
{
    public Vector3 homePosition;   // 지킬 위치 (처음 실행 시 자동 설정)
    public float guardRadius = 5f;
    public float attackRange = 1.2f;
    private bool initialized = false;

    public override void Execute(MonsterController monster)
    {
        // 최초 1회, 스폰된 위치를 지킬 지점으로 저장
        if (!initialized)
        {
            homePosition = monster.transform.position;
            initialized = true;
        }

        bool targetInZone = monster.target != null &&
            Vector2.Distance(homePosition, monster.target.position) <= guardRadius;

        if (targetInZone)
        {
            float dist = Vector2.Distance(monster.transform.position, monster.target.position);
            if (dist > attackRange)
            {
                monster.MoveTowards(monster.target.position);
            }
            else
            {
                monster.Stop();
                monster.AttackTarget();
                Debug.Log($"{monster.data?.monsterName}이 구역을 지키며 공격!");
            }
        }
        else
        {
            // 대상이 없거나 구역 밖 → 제자리로 복귀
            float distFromHome = Vector2.Distance(monster.transform.position, homePosition);
            if (distFromHome > 0.1f)
            {
                monster.MoveTowards(homePosition);
            }
            else
            {
                monster.Stop();
            }
        }
    }
}