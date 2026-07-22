using UnityEngine;

[System.Serializable]
public class MonsterAnimationSet
{
    public AnimationClip idle;
    public AnimationClip move;
    public AnimationClip attack;
    public AnimationClip hit;
    public AnimationClip death;
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

    [Header("프리팹")]
    public GameObject prefab;
}
