using UnityEngine;

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
