using UnityEngine;

// 무기 종류: 근접(범위 판정) / 원거리(투사체 발사).
public enum WeaponCategory { Melee, Ranged }

// 근접무기 공격 방식: 클릭 1회당 스윙 1회 vs 마우스를 누르고 있는 동안 연속 타격.
public enum MeleeAttackMode { SingleSwing, HoldContinuous }

// 플레이어 무기 데이터. Monster/SkillData.cs와 동일한 패턴(ScriptableObject + CreateAssetMenu)으로
// 기획자가 Inspector에서 수치를 바로 조정할 수 있게 한다.
[CreateAssetMenu(fileName = "New Weapon", menuName = "Player/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName;
    public WeaponCategory category;
    public Sprite equippedSprite;      // RightHandWeapon 슬롯에 표시될 스프라이트
    public Sprite pickupSprite;        // 필드 픽업 아이템에 표시될 스프라이트
    [Tooltip("숫자키 1~5 매핑용 슬롯 인덱스 (0~4)")]
    public int slotIndex;
    [Tooltip("기존 Animator에 이미 있는 4개 공격 스테이트 중 재생할 이름")]
    public string attackAnimState = "AttackSlash";

    [Header("표시")]
    [Tooltip("무기 스프라이트를 캐릭터 크기에 맞게 보정하는 배율(WeaponMuzzle의 localScale에 적용)")]
    public float displayScale = 1f;

    [Header("공통 전투 수치")]
    public int damage = 10;
    [Tooltip("공격 간 최소 간격(초). 연사/연타 무기의 tick 주기로도 쓰임")]
    public float cooldown = 0.5f;
    public DamageType damageType = DamageType.Normal;
    public AudioClip sfx;

    [Header("근접 전용")]
    public MeleeAttackMode meleeMode;
    public float meleeRange = 1f;
    public float meleeHitRadius = 0.6f;

    [Header("원거리 전용")]
    public GameObject bulletPrefab;
    public float projectileSpeed = 15f;
    public float projectileMaxDistance = 15f;
    [Tooltip("샷건처럼 한 번에 여러 발 나갈 때 사용")]
    public int pelletCount = 1;
    [Tooltip("펠릿이 2개 이상일 때 전체 산탄 각도(도)")]
    public float spreadAngleDeg = 0f;
    [Tooltip("관통 가능 횟수. 0이면 첫 명중 시 소멸(저격총만 1 이상)")]
    public int pierceCount = 0;
}
