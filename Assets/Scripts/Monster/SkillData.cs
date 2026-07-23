using UnityEngine;

public enum SkillType { Melee, Ranged, Buff, Debuff, Heal }

[CreateAssetMenu(fileName = "New Skill", menuName = "Monster/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;
    public int damage;
    public float cooldown;
    public GameObject effectPrefab;   // 파티클 이펙트 연결
    public AudioClip sfx;
    public SkillType type;

    // 스킬 사용 시 effectPrefab을 스폰하고 그 오브젝트에서 재생할 애니메이션.
    // (몬스터 본체 애니메이션이 아니라, 맵에 생성되는 스킬 이펙트 오브젝트에서 재생된다)
    public AnimationClip attackAnimation;
}
