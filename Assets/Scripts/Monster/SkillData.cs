using UnityEngine;

public enum SkillType { Melee, Ranged, Buff, Debuff, Heal }

[CreateAssetMenu(fileName = "New Skill", menuName = "Monster/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;
    public int damage;
    public float cooldown;
    public GameObject effectPrefab;   // 파티클 이펙트 연결. 연출(애니메이션 등)은 이 프리팹 안에서 스스로 처리한다.
    public AudioClip sfx;
    public SkillType type;

    // effectPrefab을 스폰한 뒤 이 시간(초)이 지나면 파괴한다.
    public float effectDuration = 1f;
}
