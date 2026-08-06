using UnityEngine;

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
