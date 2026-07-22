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
    public override void Execute(MonsterController monster)
    {
        // 플레이어에게 돌진하는 로직
        // 예: monster.transform.position = Vector3.MoveTowards(...)
    }
}

[CreateAssetMenu(menuName = "Monster/AI/Ranged Kiter", fileName = "AI_RangedKiter")]
public class RangedKiterAI : MonsterAIBehavior
{
    public override void Execute(MonsterController monster)
    {
        // 거리를 유지하며 원거리 공격하는 로직
    }
}

[CreateAssetMenu(menuName = "Monster/AI/Passive", fileName = "AI_Passive")]
public class PassiveAI : MonsterAIBehavior
{
    public override void Execute(MonsterController monster)
    {
        // 공격받기 전까지 가만히 있는 로직
    }
}

[CreateAssetMenu(menuName = "Monster/AI/Guardian", fileName = "AI_Guardian")]
public class GuardianAI : MonsterAIBehavior
{
    public override void Execute(MonsterController monster)
    {
        // 특정 지역을 벗어나지 않고 지키는 로직
    }
}
