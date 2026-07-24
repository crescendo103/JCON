using UnityEngine;

// 몬스터의 체력(HP) 데이터와 증감을 전담하는 컴포넌트.
// MonsterController는 전투 연출(넉백/무적/애니메이션)만 담당하고,
// 실제 HP 값 관리와 체력바 갱신은 이 컴포넌트가 맡는다.
public class MonsterHealth : MonoBehaviour
{
    private int maxHP;
    private int currentHP;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;

    private MonsterHealthBar healthBar;

    void Awake()
    {
        // 기존 프리팹을 수정하지 않아도 자동으로 체력바가 붙도록 없으면 추가한다.
        healthBar = GetComponent<MonsterHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<MonsterHealthBar>();
    }

    public void Initialize(int newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = newMaxHP;
    }

    // amount만큼 HP를 깎고 체력바를 갱신한다. HP가 0 이하가 되면 true(사망)를 반환한다.
    public bool ApplyDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        if (healthBar != null) healthBar.Show(currentHP, maxHP);

        return currentHP <= 0;
    }
}
