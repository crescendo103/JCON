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

    // effectPrefab 스폰 시 곱해줄 크기 배율(1 = 원본 크기).
    public float effectScale = 1f;

    // effectPrefab 스폰 후 파괴까지의 시간(초). effectPrefab에 Animator가 있으면 그 클립 길이(한 사이클)로
    // 런타임에 자동 대체되므로(MonsterController.SpawnSkillEffect 참고), 이 값은 애니메이션이 없는
    // 이펙트(예: 사운드/파티클만 있는 경우)에 쓰이는 폴백 값이다.
    public float effectDuration = 1f;

    // 이펙트(=데미지 판정)가 나오기까지의 선딜(초). 이 시간 동안 몬스터는 공격 모션만 재생하고
    // 판정은 아직 없으므로, 플레이어가 모션을 보고 빠져나갈 반응 시간이 된다.
    // 0이면 기존처럼 즉시 발동한다(구버전 에셋 호환).
    public float windupTime = 0f;

    // 데미지 판정 콜라이더에만 적용할 크기 배율. effectScale은 그림과 판정을 함께 키우기 때문에,
    // 화려한 이펙트를 유지하면서 판정만 좁히려면 이 값을 effectScale보다 작게 준다.
    // 0 이하면 effectScale을 그대로 따라간다(구버전 에셋 호환).
    public float hitboxScale = 0f;
}
