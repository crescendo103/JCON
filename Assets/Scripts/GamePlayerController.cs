using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GamePlayerController : MonoBehaviour, IPlayable
{
    private static readonly KeyCode[] SlotKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5
    };

    // 숫자키 1번(슬롯 0)은 항상 맨손 기본공격 전용으로 예약한다. ownedWeapons[FistsSlot]은
    // 절대 채워지지 않으므로 언제든 눌러 맨손으로 되돌아갈 수 있다.
    private const int FistsSlot = 0;

    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform model;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private GameObject scoreCanvasPrefab;

    [Space(20f)]
    [SerializeField] private float health = 100f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float fistsCooldown = 0.4f;
    [Tooltip("무기 아이콘 고정 위치(플레이어 로컬 기준). 머리 밑에 오도록 조정")]
    [SerializeField] private Vector2 weaponHeldOffset = new Vector2(0f, 0.15f);
    [Tooltip("원거리 무기 총알이 생성되는 높이(플레이어 중심 기준 위로 오프셋)")]
    [SerializeField] private float bulletSpawnHeight = 0.3f;

    private bool isAttack;
    private Vector2 input;
    private Action attack;
    private Camera mainCam;

    private float maxHealth;


    // 숫자키 1~5 = 슬롯 0~4. 슬롯 0(FistsSlot)은 맨손 전용으로 항상 비어있다.
    // 1~4는 비어있으면(null) 아직 못 주운 무기.
    private readonly GameWeaponData[] ownedWeapons = new GameWeaponData[5];
    // ownedWeapons와 같은 인덱싱(슬롯 0~4)의 현재 탄약. GameWeaponData는 픽업들이 공유하는
    // ScriptableObject라 현재 탄약을 거기에 저장할 수 없어, 런타임 상태는 여기에만 둔다.
    private readonly int[] ammoInSlot = new int[5];
    private int currentSlot = FistsSlot;
    private GameWeaponData equippedWeapon;
    private float nextAttackTime;
    private readonly List<MonsterController> meleeTargetBuffer = new List<MonsterController>();


    public Vector2 CurrentVelocity => rigid.linearVelocity;

#if UNITY_EDITOR
    private void Reset()
    {
        anim = this.GetComponent<Animator>();
        rigid = this.GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        model = this.transform.Find("Model");

        weaponRenderer = this.transform.Find("WeaponMuzzle")?.GetComponent<SpriteRenderer>();

    }
#endif

    private void Awake()
    {
        maxHealth = health;
        EquipSlot(FistsSlot);
    }

    private void Update()
    {
        if (health < 0f) return;

        GetInput();
        Move();
        HandleWeaponSwitch();
        Click();
    }

    private void FixedUpdate()
    {
        // 실제 이동은 물리 스텝(FixedUpdate)에서만 적용한다. Update에서 매 프레임 velocity를
        // 덮어쓰면 물리 엔진이 그 프레임 안에서 계산한 충돌 반응(벽에 닿아 밀려나는 등)을
        // 다음 렌더 프레임이 바로 지워버려서 벽에 파고들거나 걸리는 현상이 생긴다.
        rigid.linearVelocity = health < 0f ? Vector2.zero : input.normalized * speed;
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
        // 실제 velocity 적용은 FixedUpdate에서 한다 (물리 스텝과 어긋나 벽 충돌이 어색해지는 것을 방지).
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

        // 총 원본 아트가 오른쪽을 보고 그려져 있으므로, 왼쪽을 볼 때만(model.localScale.x > 0) 뒤집는다.
        if (weaponRenderer != null) weaponRenderer.flipX = model.localScale.x > 0f;
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
    /// 숫자키 1~5로 무기 슬롯을 전환. 1번(FistsSlot)은 항상 가능하고, 2~5는 아직 못 주운 슬롯(null)이면 무시.
    /// </summary>
    private void HandleWeaponSwitch()
    {
        for (int i = 0; i < SlotKeys.Length; i++)
        {
            if (Input.GetKeyDown(SlotKeys[i]) && (i == FistsSlot || ownedWeapons[i] != null))
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
        equippedWeapon = weapon;

        if (weaponRenderer == null) return;

        if (weapon == null)
        {
            weaponRenderer.sprite = null;
            weaponRenderer.transform.localScale = Vector3.one;
            return;
        }

        Sprite sprite;
        Color color;
        float scale;
        WeaponVisuals.Resolve(weapon.equippedSprite, weapon.displayScale, out sprite, out color, out scale);

        weaponRenderer.sprite = sprite;
        weaponRenderer.color = color;
        weaponRenderer.transform.localScale = Vector3.one * scale;

        // 무기는 더 이상 마우스를 따라 움직이지 않고, 머리 밑 고정 위치에 그대로 표시된다.
        weaponRenderer.transform.localPosition = weaponHeldOffset;
        weaponRenderer.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 필드에서 무기를 주웠을 때 호출(WeaponPickup에서 호출). 해당 슬롯에 등록하고 바로 장착한다.
    /// </summary>
    public void PickupWeapon(GameWeaponData weapon)
    {
        if (weapon == null) return;

        // FistsSlot(0)은 맨손 전용으로 예약되어 있어 주운 무기가 덮어쓰지 못하게 클램프한다.
        int slot = Mathf.Clamp(weapon.slotIndex, FistsSlot + 1, ownedWeapons.Length - 1);
        ownedWeapons[slot] = weapon;
        ammoInSlot[slot] = weapon.maxAmmo;
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
                if (!TryConsumeAmmo(weapon)) return;

                nextAttackTime = Time.time + weapon.cooldown;
                attack?.Invoke();

                anim.Play(weapon.attackAnimState, 0);
                ExecuteAttack(weapon);
                SwitchToFistsIfOutOfAmmo(weapon);
            }
            return;
        }

        // 단발(SingleSwing 근접무기) / 맨손: 기존과 동일하게 스윙 1회 후 애니메이션이 끝날 때까지 잠금.
        if (!isAttack && Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= nextAttackTime)
        {
            if (!TryConsumeAmmo(weapon)) return;

            isAttack = true;
            nextAttackTime = Time.time + (weapon != null ? weapon.cooldown : fistsCooldown);
            attack?.Invoke();

            anim.Play(weapon != null ? weapon.attackAnimState : "AttackSlash", 0);
            ExecuteAttack(weapon);
            SwitchToFistsIfOutOfAmmo(weapon);
        }
        else if (isAttack && 1f <= anim.GetCurrentAnimatorStateInfo(0).normalizedTime)
        {
            isAttack = false;
        }
    }

    /// <summary>
    /// 발사 직전 탄약을 1발 소모한다. 맨손·근접무기·maxAmmo 0 이하(무제한)는 항상 true.
    /// 원거리 무기이고 탄약이 0이면 false를 반환해 발사 자체를 취소시킨다.
    /// </summary>
    private bool TryConsumeAmmo(GameWeaponData weapon)
    {
        if (weapon == null || weapon.category != WeaponCategory.Ranged || weapon.maxAmmo <= 0) return true;

        if (ammoInSlot[currentSlot] <= 0) return false;

        ammoInSlot[currentSlot]--;
        return true;
    }

    /// <summary>
    /// 마지막 탄을 쏜 직후 호출. 빈 총을 들고 딜레이 없이 계속 전투할 수 있게 맨손으로 되돌린다.
    /// 무기는 슬롯에 남아 있어, 같은 무기를 다시 주우면 탄약이 충전된다.
    /// </summary>
    private void SwitchToFistsIfOutOfAmmo(GameWeaponData weapon)
    {
        if (weapon == null || weapon.category != WeaponCategory.Ranged || weapon.maxAmmo <= 0) return;
        if (ammoInSlot[currentSlot] > 0) return;

        EquipSlot(FistsSlot);
    }

    /// <summary>
    /// 무기 종류에 따라 실제 데미지 판정을 실행한다. weapon이 null이면 맨손 근접 공격.
    /// </summary>
    private void ExecuteAttack(GameWeaponData weapon)
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

    private void DoMeleeAttack(GameWeaponData weapon)
    {
        int dmg = weapon != null ? weapon.damage : 5;
        DamageType type = weapon != null ? weapon.damageType : DamageType.Normal;
        float range = weapon != null ? weapon.meleeRange : 0.8f;
        float radius = weapon != null ? weapon.meleeHitRadius : 0.6f;
        int maxTargets = weapon != null ? weapon.maxTargets : 1;

        // model.localScale.x < 0 이면 오른쪽을 보고 있는 상태(Face() 참고).
        Vector2 facing = (model != null && model.localScale.x < 0f) ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + facing * range;

        var hits = Physics2D.OverlapCircleAll(origin, radius);
        CollectMeleeTargets(hits, origin, maxTargets, meleeTargetBuffer);

        foreach (var monster in meleeTargetBuffer)
        {
            monster.TakeDamage(dmg, type, transform.position);
        }
    }

    /// <summary>
    /// hits 중 몬스터만 골라 origin에 가까운 순으로 정렬해 results에 담는다(중복 제거).
    /// maxTargets가 1이면 가장 가까운 하나만(기존 단일 타격 무기와 동일한 동작),
    /// 0 이하면 범위 내 전원, 그 외에는 가까운 순으로 maxTargets명까지만 남긴다.
    /// </summary>
    private static void CollectMeleeTargets(Collider2D[] hits, Vector2 origin, int maxTargets, List<MonsterController> results)
    {
        results.Clear();

        foreach (var hit in hits)
        {
            var monster = hit.GetComponent<MonsterController>();
            if (monster == null || results.Contains(monster)) continue;

            results.Add(monster);
        }

        results.Sort((a, b) =>
        {
            float da = ((Vector2)a.transform.position - origin).sqrMagnitude;
            float db = ((Vector2)b.transform.position - origin).sqrMagnitude;
            return da.CompareTo(db);
        });

        if (maxTargets > 0 && results.Count > maxTargets)
        {
            results.RemoveRange(maxTargets, results.Count - maxTargets);
        }
    }

    private void DoRangedAttack(GameWeaponData weapon)
    {
        if (weapon.bulletPrefab == null) return;

        Vector2 aimDir = (Vector2)GetMouseWorld() - (Vector2)transform.position;
        aimDir = aimDir.sqrMagnitude > 0.0001f ? aimDir.normalized : Vector2.right;

        int pellets = Mathf.Max(1, weapon.pelletCount);
        // 무기가 더 이상 조준 방향을 가리키지 않으므로(머리 밑 고정 표시), 총알은 플레이어 중심에서 나간다.
        Vector3 spawnPos = transform.position + Vector3.up * bulletSpawnHeight;

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
            var projectile = bullet.GetComponent<GameProjectile>();
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

        if (health < 0f)
        {
            anim.Play("Death", 0);
            SpawnScoreCanvas();
        }
        else anim.Play("Hit", 0);
    }

    private void SpawnScoreCanvas()
    {
        if (scoreCanvasPrefab == null) return;

        // CrosshairUI가 Cursor.visible을 false로 숨겨둔 채라, 결과 화면 버튼을 눌러도 커서가 안 보였다.
        Cursor.visible = true;

        var scoreCanvas = Instantiate(scoreCanvasPrefab);
        scoreCanvas.SetActive(true);
    }

    /// <summary>
    /// 무기 추가 (죽을 경우 이벤트 구독 해제 해줘야함)
    /// </summary>
    /// <param name="attackEvent"></param>
    public void AddWeapon(Action attackEvent)
    {
        attack += attackEvent;
    }

    public float GetHealth() { return health; }
    public float GetMaxHealth() { return maxHealth; }

    /// <summary>
    /// 현재 장착 무기의 탄약. 탄약 개념이 없는 무기(맨손·근접·무제한)면 false를 반환한다.
    /// </summary>
    public bool TryGetAmmo(out int current, out int max)
    {
        var weapon = ownedWeapons[currentSlot];
        if (weapon == null || weapon.category != WeaponCategory.Ranged || weapon.maxAmmo <= 0)
        {
            current = 0;
            max = 0;
            return false;
        }

        current = ammoInSlot[currentSlot];
        max = weapon.maxAmmo;
        return true;
    }
}
