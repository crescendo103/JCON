using UnityEngine;

/// <summary>
/// 실제 몬스터 오브젝트에 붙는 컴포넌트.
/// MonsterData를 참조해서 스탯/AI/스킬을 실행합니다.
/// 지금은 최소 뼈대만 있고, 실제 이동/전투 로직은 프로젝트에 맞게 채워 넣으면 됩니다.
/// </summary>
public class MonsterController : MonoBehaviour
{
    public MonsterData data;
    private int currentHP;

    void Start()
    {
        if (data != null)
            currentHP = data.maxHP;
    }

    void Update()
    {
        if (data != null && data.aiBehavior != null)
        {
            data.aiBehavior.Execute(this);
        }
    }
}
