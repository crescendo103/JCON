using UnityEngine;

[System.Serializable]
public class MonsterAnimationSet
{
    public AnimationClip idle;
    public Motion move;
    public Motion attack;
    public Motion hit;
    public AnimationClip death;
}

// 피격 시 데미지 종류(DamageType)별 넉백 세기/지속시간과 무적 시간을 지정한다.
// MonsterController.TakeDamage가 damageType에 맞는 항목을 찾아 사용한다.
[System.Serializable]
public class KnockbackSetting
{
    public DamageType type;
    public float force = 3f;
    public float duration = 0.15f;
    public float invincibilityDuration = 0.5f;
}

[CreateAssetMenu(fileName = "New Monster", menuName = "Monster/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName;
    public Sprite icon;

    [Header("스탯")]
    public int maxHP;
    public int attack;
    public int defense;
    public float speed;

    [Header("스킬")]
    public SkillData[] skills;

    [Header("애니메이션")]
    public MonsterAnimationSet animations;

    [Header("AI")]
    public MonsterAIBehavior aiBehavior;

    [Header("피격 반응 (넉백/무적시간)")]
    public KnockbackSetting[] knockbackSettings = new KnockbackSetting[]
    {
        new KnockbackSetting { type = DamageType.Light, force = 1.5f, duration = 0.1f, invincibilityDuration = 0.3f },
        new KnockbackSetting { type = DamageType.Normal, force = 3f, duration = 0.15f, invincibilityDuration = 0.5f },
        new KnockbackSetting { type = DamageType.Heavy, force = 6f, duration = 0.25f, invincibilityDuration = 1f },
    };

    [Header("프리팹")]
    public GameObject prefab;
}
