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
    private BossHealthBar bossHealthBar;

    void Awake()
    {
        // 보스 프리팹에 BossHealthBar가 미리 붙어 있으면 그걸 쓰고, 머리 위 체력바(MonsterHealthBar)는
        // 붙이지 않는다. 없으면(일반 몬스터) 기존처럼 자동으로 MonsterHealthBar를 붙여준다.
        bossHealthBar = GetComponent<BossHealthBar>();
        if (bossHealthBar == null)
        {
            healthBar = GetComponent<MonsterHealthBar>();
            if (healthBar == null) healthBar = gameObject.AddComponent<MonsterHealthBar>();
        }
    }

    public void Initialize(int newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = newMaxHP;

        if (bossHealthBar != null) bossHealthBar.Show(currentHP, maxHP);
    }

    // amount만큼 HP를 깎고 체력바를 갱신한다. HP가 0 이하가 되면 true(사망)를 반환한다.
    public bool ApplyDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        if (bossHealthBar != null) bossHealthBar.Show(currentHP, maxHP);
        else if (healthBar != null) healthBar.Show(currentHP, maxHP);

        return currentHP <= 0;
    }
}
