using UnityEngine;

// 무기 종류: 근접(범위 판정) / 원거리(투사체 발사).
public enum WeaponCategory { Melee, Ranged }

// 근접무기 공격 방식: 클릭 1회당 스윙 1회 vs 마우스를 누르고 있는 동안 연속 타격.
public enum MeleeAttackMode { SingleSwing, HoldContinuous }

// 플레이어 무기 데이터. Monster/SkillData.cs와 동일한 패턴(ScriptableObject + CreateAssetMenu)으로
// 기획자가 Inspector에서 수치를 바로 조정할 수 있게 한다.
[CreateAssetMenu(fileName = "New Weapon", menuName = "Player/Weapon Data")]
public class GameWeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName;
    public WeaponCategory category;
    public Sprite equippedSprite;      // RightHandWeapon 슬롯에 표시될 스프라이트
    public Sprite pickupSprite;        // 필드 픽업 아이템에 표시될 스프라이트
    [Tooltip("equippedSprite가 비어 있을 때 코드로 그린 전기톱 아이콘을 자동으로 쓸지 여부. 실제 아트가 생겨 equippedSprite를 채우면 이 값과 무관하게 그 아트가 항상 우선된다")]
    public bool useProceduralChainsawIcon;
    [Tooltip("숫자키 1~5 매핑용 슬롯 인덱스 (0~4)")]
    public int slotIndex;
    [Tooltip("기존 Animator에 이미 있는 4개 공격 스테이트 중 재생할 이름")]
    public string attackAnimState = "AttackSlash";

    [Header("표시")]
    [Tooltip("무기 스프라이트를 캐릭터 크기에 맞게 보정하는 배율(WeaponMuzzle의 localScale에 적용)")]
    public float displayScale = 1f;
    [Tooltip("장착 시 스폰되는 이 무기 전용 비주얼 프리팹. SpriteRenderer + 총구 위치를 표시하는 자식 Muzzle 오브젝트를 가진다. 총마다 총구 위치가 달라 무기별로 따로 둔다")]
    public GameObject weaponVisualPrefab;

    [Header("공통 전투 수치")]
    public int damage = 10;
    [Tooltip("공격 간 최소 간격(초). 연사/연타 무기의 tick 주기로도 쓰임")]
    public float cooldown = 0.5f;
    public DamageType damageType = DamageType.Normal;
    public AudioClip sfx;
    [Tooltip("탄약(내구도)이 다 닳아 무기가 부서지며 맨손으로 돌아갈 때 재생되는 사운드")]
    public AudioClip breakSfx;

    [Header("근접 전용")]
    public MeleeAttackMode meleeMode;
    public float meleeRange = 1f;
    public float meleeHitRadius = 0.6f;
    [Tooltip("한 번의 공격으로 타격할 최대 대상 수. 1이면 가장 가까운 하나만, 0 이하면 범위 내 전원")]
    public int maxTargets = 1;

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
    [Tooltip("탄창 최대 탄약 수. 0 이하면 무제한(근접무기/맨손). 발사 1회당 1발 소모(샷건 펠릿은 1발로 계산)")]
    public int maxAmmo = 0;
}
