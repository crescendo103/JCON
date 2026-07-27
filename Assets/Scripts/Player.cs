using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IPlayable
{
    private static readonly KeyCode[] SlotKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5
    };

    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform model;
    [SerializeField] private SpriteRenderer weaponRenderer;

    [Space(20f)]
    [SerializeField] private float health = 100f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float fistsCooldown = 0.4f;
    [SerializeField] private float muzzleHoldDistance = 0.35f;

    private bool isAttack;
    private Vector2 input;
    private Action attack;
    private Camera mainCam;
<<<<<<< Updated upstream
    private float maxHealth;
=======

    // 숫자키 1~5 = 슬롯 0~4. 비어있으면(null) 아직 못 주운 무기.
    private readonly WeaponData[] ownedWeapons = new WeaponData[5];
    private int currentSlot = -1;
    private float nextAttackTime;

>>>>>>> Stashed changes
    public Vector2 CurrentVelocity => rigid.linearVelocity;

#if UNITY_EDITOR
    private void Reset()
    {
        anim = this.GetComponent<Animator>();
        rigid = this.GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        model = this.transform.Find("Model");
<<<<<<< Updated upstream

        maxHealth = health;
=======
        weaponRenderer = this.transform.Find("WeaponMuzzle")?.GetComponent<SpriteRenderer>();
>>>>>>> Stashed changes
    }
#endif

    private void Update()
    {
        if (health < 0f) return;

        GetInput();
        Move();
        HandleWeaponSwitch();
        AimWeapon();
        Click();
        Test();
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.A)) input.x = -1f;
        else if (Input.GetKey(KeyCode.D)) input.x = 1f;
        else input.x = 0f;

        if (Input.GetKey(KeyCode.W)) input.y = 1f;
        else if (Input.GetKey(KeyCode.S)) input.y = -1f;
        else input.y = 0f;
    }

    private void Move()
    {
        if (!isAttack)
        {
            if(input != Vector2.zero) anim.Play("Run", 0);
            else anim.Play("Idle", 0);
        }

        Face();
        rigid.linearVelocity = input.normalized * speed;
    }

    /// <summary>
    /// 마우스 포인터가 있는 쪽을 바라보게 함 (스프라이트가 정면 단일 방향이라 좌우 반전만 처리)
    /// </summary>
    private void Face()
    {
        if (model == null) return;

        float dx = GetMouseWorld().x - transform.position.x;

        if (dx > 0f) model.localScale = new Vector3(-1f, 1f, 1f);
        else if (dx < 0f) model.localScale = new Vector3(1f, 1f, 1f);
    }

    /// <summary>
    /// 마우스 스크린 좌표를 world 좌표로 변환. 카메라가 없으면 자기 위치를 반환(호출부에서 dx=0 처리됨).
    /// </summary>
    private Vector3 GetMouseWorld()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return transform.position;

        return mainCam.ScreenToWorldPoint(Input.mousePosition);
    }

    /// <summary>
    /// 숫자키 1~5로 보유 중인 무기 슬롯을 전환. 아직 못 주운 슬롯(null)은 무시.
    /// </summary>
    private void HandleWeaponSwitch()
    {
        for (int i = 0; i < SlotKeys.Length; i++)
        {
            if (Input.GetKeyDown(SlotKeys[i]) && ownedWeapons[i] != null)
            {
                EquipSlot(i);
                break;
            }
        }
    }

    private void EquipSlot(int slot)
    {
        currentSlot = slot;
        var weapon = ownedWeapons[slot];

        if (weaponRenderer != null)
        {
            weaponRenderer.sprite = weapon?.equippedSprite;
            weaponRenderer.transform.localScale = Vector3.one * (weapon != null ? weapon.displayScale : 1f);
        }
    }

    /// <summary>
    /// 무기(WeaponMuzzle)가 항상 마우스 포인터 방향을 정확히 가리키도록 회전/위치를 갱신한다.
    /// WeaponMuzzle은 Model(좌우반전용) 밖의 독립 트랜스폼이라 반전 보정 없이 월드 각도를 그대로 적용하면 된다.
    /// </summary>
    private void AimWeapon()
    {
        if (weaponRenderer == null) return;

        Vector2 aimDir = (Vector2)GetMouseWorld() - (Vector2)transform.position;
        aimDir = aimDir.sqrMagnitude > 0.0001f ? aimDir.normalized : Vector2.right;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        weaponRenderer.transform.position = (Vector2)transform.position + aimDir * muzzleHoldDistance;
        weaponRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 필드에서 무기를 주웠을 때 호출(WeaponPickup에서 호출). 해당 슬롯에 등록하고 바로 장착한다.
    /// </summary>
    public void PickupWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        int slot = Mathf.Clamp(weapon.slotIndex, 0, ownedWeapons.Length - 1);
        ownedWeapons[slot] = weapon;
        EquipSlot(slot);
    }

    private void Click()
    {
        var weapon = currentSlot >= 0 ? ownedWeapons[currentSlot] : null;
        bool holdFire = weapon != null && (weapon.category == WeaponCategory.Ranged || weapon.meleeMode == MeleeAttackMode.HoldContinuous);

        if (holdFire)
        {
            // 연사(원거리)/연타(전기톱): 애니메이션 완료를 기다리지 않고 쿨다운으로만 속도를 제어.
            // isAttack을 건드리지 않으므로 이동/Idle-Run 애니메이션도 그대로 유지된다.
            if (Input.GetKey(KeyCode.Mouse0) && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + weapon.cooldown;
                attack?.Invoke();

                anim.Play(weapon.attackAnimState, 0);
                ExecuteAttack(weapon);
            }
            return;
        }

        // 단발(야구방망이) / 맨손: 기존과 동일하게 스윙 1회 후 애니메이션이 끝날 때까지 잠금.
        if (!isAttack && Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= nextAttackTime)
        {
            isAttack = true;
            nextAttackTime = Time.time + (weapon != null ? weapon.cooldown : fistsCooldown);
            attack?.Invoke();

            anim.Play(weapon != null ? weapon.attackAnimState : "AttackSlash", 0);
            ExecuteAttack(weapon);
        }
        else if (isAttack && 1f <= anim.GetCurrentAnimatorStateInfo(0).normalizedTime)
        {
            isAttack = false;
        }
    }

    /// <summary>
    /// 무기 종류에 따라 실제 데미지 판정을 실행한다. weapon이 null이면 맨손 근접 공격.
    /// </summary>
    private void ExecuteAttack(WeaponData weapon)
    {
        if (weapon == null || weapon.category == WeaponCategory.Melee)
        {
            DoMeleeAttack(weapon);
        }
        else
        {
            DoRangedAttack(weapon);
        }
    }

    private void DoMeleeAttack(WeaponData weapon)
    {
        int dmg = weapon != null ? weapon.damage : 5;
        DamageType type = weapon != null ? weapon.damageType : DamageType.Normal;
        float range = weapon != null ? weapon.meleeRange : 0.8f;
        float radius = weapon != null ? weapon.meleeHitRadius : 0.6f;

        // model.localScale.x < 0 이면 오른쪽을 보고 있는 상태(Face() 참고).
        Vector2 facing = (model != null && model.localScale.x < 0f) ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + facing * range;

        // 판정 범위 안에 여러 몬스터가 있어도 가장 가까운 하나만 타격한다.
        var hits = Physics2D.OverlapCircleAll(origin, radius);
        MonsterController closest = null;
        float closestSqrDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var monster = hit.GetComponent<MonsterController>();
            if (monster == null) continue;

            float sqrDist = ((Vector2)monster.transform.position - origin).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = monster;
            }
        }

        closest?.TakeDamage(dmg, type, transform.position);
    }

    private void DoRangedAttack(WeaponData weapon)
    {
        if (weapon.bulletPrefab == null) return;

        Vector2 aimDir = (Vector2)GetMouseWorld() - (Vector2)transform.position;
        aimDir = aimDir.sqrMagnitude > 0.0001f ? aimDir.normalized : Vector2.right;

        int pellets = Mathf.Max(1, weapon.pelletCount);
        Vector3 spawnPos = weaponRenderer != null ? weaponRenderer.transform.position : transform.position;

        for (int i = 0; i < pellets; i++)
        {
            float angleOffset = 0f;
            if (pellets > 1)
            {
                float t = (float)i / (pellets - 1);
                angleOffset = Mathf.Lerp(-weapon.spreadAngleDeg * 0.5f, weapon.spreadAngleDeg * 0.5f, t);
            }

            Vector2 dir = Quaternion.Euler(0f, 0f, angleOffset) * aimDir;

            var bullet = Instantiate(weapon.bulletPrefab, spawnPos, Quaternion.identity);
            var projectile = bullet.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.speed = weapon.projectileSpeed;
                projectile.maxDistance = weapon.projectileMaxDistance;
                projectile.Launch(dir, weapon.damage, weapon.damageType, weapon.pierceCount);
            }
        }
    }

    public void Hit(float dmg)
    {
        if (health < 0f) return;
        health -= dmg;

        if (health < 0f) anim.Play("Death", 0);
        else anim.Play("Hit", 0);
    }

    /// <summary>
    /// 무기 추가 (죽을 경우 이벤트 구독 해제 해줘야함)
    /// </summary>
    /// <param name="attackEvent"></param>
    public void AddWeapon(Action attackEvent)
    {
        attack += attackEvent;
    }

    private void Test()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Hit(20f);
        }
    }
    
    public float GetHealth() { return health; }
    public float GetMaxHealth() { return maxHealth; }
}
