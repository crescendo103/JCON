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
}
