using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    // Pixem 파츠 잔재라 sortingOrder가 Body와 같은 0이라 몸통에 가려진다. WeaponMuzzle과 같은 레벨로 끌어올린다.
    private const int HandWeaponSortingOrder = 4;

    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private Animator anim;
    [Tooltip("무기 발사/공격 사운드 재생용. 발자국 소리(PlayerFootstepSound)와 같은 AudioSource를 공유해도 된다")]
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private Transform model;
    [Tooltip("장착 무기 비주얼이 생성될 앵커(머리 밑 고정 위치)")]
    [SerializeField] private Transform weaponSocket;
    [Tooltip("맨손일 때 검을 표시할 렌더러 (Model/LeftHand/LeftHandWeapon)")]
    [SerializeField] private SpriteRenderer handWeaponRenderer;
    [Tooltip("캐릭터 손 위치(Model 로컬). Idle 프레임 손 픽셀(x39,row48) 실측값. 크기/기울기와 무관하게 여기만 맞추면 된다")]
    [SerializeField] private Vector2 fistsSwordHandOffset = new Vector2(0.2344f, -0.5156f);
    [Tooltip("검 3프레임(Sward 1,2,3 순서)의 손잡이 위치. 스프라이트 피벗에서 손잡이 중심까지의 거리(월드 단위, 배율 1 기준). 아트에서 픽셀로 실측한 값이라 웬만하면 안 건드려도 됨")]
    [SerializeField] private Vector2[] fistsSwordGripOffsets =
    {
        new Vector2(-0.02f, -0.12f),
        new Vector2(-0.12f,  0.02f),
        new Vector2( 0.02f,  0.12f),
    };
    [Tooltip("검 아트가 진행 방향과 반대로(칼끝이 등 뒤로) 그려져 있어 기본은 켜서 좌우로 뒤집는다")]
    [SerializeField] private bool fistsSwordFlipX = true;
    [Tooltip("맨손 검 기울기(도). 0이면 스프라이트 원래 각도")]
    [SerializeField] private float fistsSwordTiltDeg = 0f;
    [Tooltip("맨손 검 크기 배율. 검 아트는 PPU 25라 캐릭터(PPU 32)와 픽셀 밀도를 맞추려면 25/32 = 0.78125")]
    [SerializeField] private float fistsSwordScale = 0.78125f;
    [Tooltip("맨손 검 휘두르기 1회에 걸리는 시간(초). 검 3프레임을 이 시간에 나눠 재생한다. AttackSlash 클립 길이(0.3초)에 맞춰둠")]
    [SerializeField] private float fistsSwordSwingDuration = 0.3f;
    [SerializeField] private GameObject scoreCanvasPrefab;
    [SerializeField] private PlayerHitVignette hitVignette;

    [Space(20f)]
    [SerializeField] private float health = 100f;
    [SerializeField] private float speed = 1f;
    [Tooltip("Shift를 누르고 있는 동안 speed에 곱해지는 배율")]
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [Tooltip("스태미나 최대치")]
    [SerializeField] private float staminaMax = 100f;
    [Tooltip("스프린트 중 초당 소모량")]
    [SerializeField] private float staminaDrainPerSecond = 25f;
    [Tooltip("스프린트를 안 쓰는 동안 초당 회복량")]
    [SerializeField] private float staminaRegenPerSecond = 15f;
    [SerializeField] private float fistsCooldown = 0.4f;
    [Tooltip("맨손(기본 공격) 휘두를 때 재생되는 사운드")]
    [SerializeField] private AudioClip fistsAttackSfx;
    [Tooltip("무기 아이콘 고정 위치(플레이어 로컬 기준). 머리 밑에 오도록 조정")]
    [SerializeField] private Vector2 weaponHeldOffset = new Vector2(0f, 0.15f);
    [Tooltip("원거리 무기 총알이 생성되는 높이(플레이어 중심 기준 위로 오프셋)")]
    [SerializeField] private float bulletSpawnHeight = 0.3f;
    [Tooltip("원거리 무기 발사 시 현재 장착된 무기 비주얼의 Muzzle 위치에서 재생되는 총구 이펙트. 애니메이션 한 바퀴 끝나면 EffectAutoDestroy가 알아서 없앤다")]
    [SerializeField] private GameObject muzzleEffectPrefab;

    private bool isAttack;
    private bool isSprinting;
    private Vector2 input;
    private Action attack;
    private Camera mainCam;

    private float maxHealth;
    private float stamina;
    // 스태미나가 0이 되면 켜지고, 100% 회복될 때까지 꺼지지 않는다("다 닳으면 다 찰 때까지 못 달림").
    private bool staminaExhausted;


    // 숫자키 1~5 = 슬롯 0~4. 슬롯 0(FistsSlot)은 맨손 전용으로 항상 비어있다.
    // 1~4는 비어있으면(null) 아직 못 주운 무기.
    private readonly GameWeaponData[] ownedWeapons = new GameWeaponData[5];
    // ownedWeapons와 같은 인덱싱(슬롯 0~4)의 현재 탄약. GameWeaponData는 픽업들이 공유하는
    // ScriptableObject라 현재 탄약을 거기에 저장할 수 없어, 런타임 상태는 여기에만 둔다.
    private readonly int[] ammoInSlot = new int[5];
    private int currentSlot = FistsSlot;
    private GameWeaponData equippedWeapon;
    // 장착 시 weaponVisualPrefab을 스폰해 만들어지고, 무기를 바꾸거나 벗을 때 파괴된다.
    private GameObject weaponVisualInstance;
    private SpriteRenderer weaponRenderer;
    private Transform weaponMuzzle;
    // 좌우 반전 전의 원래 크기. UpdateWeaponFacing()이 매 프레임 부호만 바꿔 다시 곱한다.
    private float weaponVisualBaseScale = 1f;
    private float nextAttackTime;
    // 플레이어 전체가 이 SortingGroup(레이어 BuildingPlayer)으로 한 덩어리로 그려진다. 그룹 안에 든
    // 렌더러의 sortingLayerID(Default)는 그룹 안 순서에만 쓰이고 그룹 밖에서는 무의미하므로,
    // 그룹 밖에 스폰되는 이펙트를 앞에 그리려면 이 그룹 자체의 레이어/순서를 기준으로 삼아야 한다.
    private SortingGroup sortingGroup;
    private readonly List<MonsterController> meleeTargetBuffer = new List<MonsterController>();
    // 맨손 검 휘두르기 진행 상태. 휘두르는 동안에만 true이고, 끝나면 기본 자세(0번 프레임)로 돌아간다.
    private bool fistsSwordSwinging;
    private float fistsSwordSwingStart;


    public Vector2 CurrentVelocity => rigid.linearVelocity;

#if UNITY_EDITOR
    private void Reset()
    {
        anim = this.GetComponent<Animator>();
        rigid = this.GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        model = this.transform.Find("Model");

        weaponSocket = this.transform.Find("WeaponMuzzle");
        hitVignette = this.GetComponent<PlayerHitVignette>();
        handWeaponRenderer = this.transform.Find("Model/LeftHand/LeftHandWeapon")?.GetComponent<SpriteRenderer>();
        weaponAudioSource = this.GetComponent<AudioSource>();

    }
#endif

    private void Awake()
    {
        if (hitVignette == null) hitVignette = GetComponent<PlayerHitVignette>();
        if (weaponAudioSource == null) weaponAudioSource = GetComponent<AudioSource>();
        sortingGroup = GetComponent<SortingGroup>();

        maxHealth = health;
        stamina = staminaMax;

        // 프리팹에 직렬화된 값이 없어도 동작하도록 런타임에서도 경로로 찾는다(README: 코드로 연결).
        if (handWeaponRenderer == null)
            handWeaponRenderer = transform.Find("Model/LeftHand/LeftHandWeapon")?.GetComponent<SpriteRenderer>();

        if (handWeaponRenderer != null) handWeaponRenderer.sortingOrder = HandWeaponSortingOrder;

        // 무기는 더 이상 마우스를 따라 움직이지 않고, 머리 밑 고정 위치에 그대로 표시된다.
        if (weaponSocket != null) weaponSocket.localPosition = weaponHeldOffset;

        EquipSlot(FistsSlot);
    }

    private void Update()
    {
        if (health <= 0f || StageManager.IsGameOver) return;

        GetInput();
        UpdateStamina();
        Move();
        HandleWeaponSwitch();
        Click();
        TickFistsSword();
    }

    private void FixedUpdate()
    {
        // 실제 이동은 물리 스텝(FixedUpdate)에서만 적용한다. Update에서 매 프레임 velocity를
        // 덮어쓰면 물리 엔진이 그 프레임 안에서 계산한 충돌 반응(벽에 닿아 밀려나는 등)을
        // 다음 렌더 프레임이 바로 지워버려서 벽에 파고들거나 걸리는 현상이 생긴다.
        float currentSpeed = isSprinting ? speed * sprintSpeedMultiplier : speed;
        rigid.linearVelocity = (health <= 0f || StageManager.IsGameOver) ? Vector2.zero : input.normalized * currentSpeed;
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.A)) input.x = -1f;
        else if (Input.GetKey(KeyCode.D)) input.x = 1f;
        else input.x = 0f;

        if (Input.GetKey(KeyCode.W)) input.y = 1f;
        else if (Input.GetKey(KeyCode.S)) input.y = -1f;
        else input.y = 0f;

        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    /// <summary>
    /// 스프린트 중 스태미나를 소모하고, 안 쓰는 동안 회복한다. 0이 되면 소진 상태로 잠기고
    /// 100%로 완전히 회복되기 전까지는(부분 회복만으로는) 다시 스프린트할 수 없다.
    /// isSprinting은 GetInput()이 저장한 "Shift 눌림" 원값을 여기서 실제 가능 여부로 덮어써서,
    /// FixedUpdate의 속도 계산은 그대로 isSprinting만 보면 되게 한다.
    /// </summary>
    private void UpdateStamina()
    {
        bool wantsSprint = isSprinting;
        bool activelySprinting = wantsSprint && !staminaExhausted && input != Vector2.zero;

        if (activelySprinting)
        {
            stamina -= staminaDrainPerSecond * Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                staminaExhausted = true;
            }
        }
        else
        {
            stamina += staminaRegenPerSecond * Time.deltaTime;
            if (stamina >= staminaMax)
            {
                stamina = staminaMax;
                staminaExhausted = false;
            }
        }

        stamina = Mathf.Clamp(stamina, 0f, staminaMax);
        isSprinting = wantsSprint && !staminaExhausted;
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

        UpdateWeaponFacing();
    }

    // weaponRenderer.flipX만 뒤집으면 렌더링만 반전되고 Muzzle 같은 자식 Transform의 위치는 그대로라
    // 총구가 캐릭터가 보는 방향과 반대편에 고정돼버린다. weaponVisualInstance 자체를 좌우 반전시켜야
    // 스프라이트와 Muzzle 위치가 같이 뒤집힌다.
    private void UpdateWeaponFacing()
    {
        if (weaponVisualInstance == null || model == null) return;

        // 총 원본 아트가 오른쪽을 보고 그려져 있으므로, 왼쪽을 볼 때만(model.localScale.x > 0) 뒤집는다.
        bool flip = model.localScale.x > 0f;
        Vector3 scale = weaponVisualInstance.transform.localScale;
        weaponVisualInstance.transform.localScale = new Vector3(flip ? -weaponVisualBaseScale : weaponVisualBaseScale, scale.y, scale.z);
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

        UpdateFistsSword(weapon == null);

        DestroyWeaponVisual();

        if (weapon != null) SpawnWeaponVisual(weapon);
    }

    private void DestroyWeaponVisual()
    {
        if (weaponVisualInstance == null) return;

        Destroy(weaponVisualInstance);
        weaponVisualInstance = null;
        weaponRenderer = null;
        weaponMuzzle = null;
    }

    // weaponSocket(머리 밑 고정 앵커) 아래에 무기 전용 비주얼 프리팹을 스폰하고, 그 안의 SpriteRenderer/Muzzle을 캐시해둔다.
    // 총구 위치가 무기마다 달라 비주얼 프리팹도 weapon.weaponVisualPrefab으로 무기별로 따로 둔다.
    private void SpawnWeaponVisual(GameWeaponData weapon)
    {
        if (weapon.weaponVisualPrefab == null || weaponSocket == null) return;

        weaponVisualInstance = Instantiate(weapon.weaponVisualPrefab, weaponSocket);
        weaponVisualInstance.transform.localPosition = Vector3.zero;
        weaponVisualInstance.transform.localRotation = Quaternion.identity;

        weaponRenderer = weaponVisualInstance.GetComponentInChildren<SpriteRenderer>();
        weaponMuzzle = weaponVisualInstance.transform.Find("Muzzle");

        Sprite equipSource = weapon.equippedSprite;
        if (equipSource == null && weapon.useProceduralChainsawIcon) equipSource = WeaponVisuals.ChainsawIcon;

        Sprite sprite;
        Color color;
        float scale;
        WeaponVisuals.Resolve(equipSource, weapon.displayScale, out sprite, out color, out scale);

        if (weaponRenderer != null)
        {
            weaponRenderer.sprite = sprite;
            weaponRenderer.color = color;
        }

        weaponVisualBaseScale = scale;
        weaponVisualInstance.transform.localScale = Vector3.one * scale;
        UpdateWeaponFacing();
    }

    /// <summary>
    /// 장착이 바뀔 때 손의 검을 켜고 끈다. 무기를 들면 WeaponMuzzle 아이콘이 대신하므로 숨긴다.
    /// 실제 프레임 갱신은 매 프레임 TickFistsSword()가 맡는다.
    /// </summary>
    private void UpdateFistsSword(bool unarmed)
    {
        if (handWeaponRenderer == null) return;

        // 무기를 바꾸면 휘두르던 동작은 취소하고 기본 자세부터 다시 시작한다.
        fistsSwordSwinging = false;

        if (!unarmed)
        {
            handWeaponRenderer.sprite = null;
            return;
        }

        ApplyFistsSwordFrame(0);
    }

    /// <summary>
    /// 맨손일 때 손에 든 검의 프레임을 매 프레임 갱신한다. 휘두르는 중이면 경과 시간에 따라
    /// Sward 1(치켜듦) → 2(휘두름) → 3(내려침)을 차례로 보여주고, 끝나면 기본 자세로 되돌린다.
    /// </summary>
    private void TickFistsSword()
    {
        if (handWeaponRenderer == null || equippedWeapon != null) return;

        int frameCount = WeaponVisuals.FistsSwordSprites.Length;
        int frame = 0;

        if (fistsSwordSwinging && frameCount > 0)
        {
            float progress = (Time.time - fistsSwordSwingStart) / Mathf.Max(0.01f, fistsSwordSwingDuration);

            if (progress >= 1f) fistsSwordSwinging = false;
            else frame = Mathf.Clamp((int)(progress * frameCount), 0, frameCount - 1);
        }

        ApplyFistsSwordFrame(frame);
    }

    /// <summary>
    /// 검 스프라이트 한 프레임을 손 위치에 맞춰 배치한다.
    /// 노드의 원점은 스프라이트 피벗이지 손잡이가 아니고, 그 간격이 프레임마다 다르다(검이 손잡이를
    /// 축으로 돌기 때문). 그래서 프레임별 손잡이 간격(fistsSwordGripOffsets)을 배율·반전·기울기까지
    /// 반영해 역으로 빼준다. 덕분에 어떤 프레임이든, 크기나 각도를 바꾸든 손잡이는 항상 손에 붙어
    /// 있어 휘두르는 동안 검이 손에서 떨어지지 않는다. Model의 자식이라 좌우 반전(캐릭터가 도는 것)은
    /// Face()가 캐릭터 전체를 뒤집을 때 자동으로 따라온다 — fistsSwordFlipX는 그것과 별개로, 검 아트
    /// 자체가 진행 방향과 반대로 그려져 있는 것을 바로잡는 고정 보정이다.
    /// (베이크된 캐릭터 시트가 64x64 프레임 안에서 우하단으로 치우쳐 있어 손 좌표 자체도 (0,0)이 아니다.)
    /// </summary>
    private void ApplyFistsSwordFrame(int index)
    {
        var sprites = WeaponVisuals.FistsSwordSprites;
        if (sprites.Length == 0) return;

        index = Mathf.Clamp(index, 0, sprites.Length - 1);
        Vector2 gripOffset = index < fistsSwordGripOffsets.Length ? fistsSwordGripOffsets[index] : Vector2.zero;

        var scale = new Vector3(fistsSwordFlipX ? -fistsSwordScale : fistsSwordScale, fistsSwordScale, 1f);
        var tilt = Quaternion.Euler(0f, 0f, fistsSwordTiltDeg);
        Vector3 gripFromPivot = tilt * Vector3.Scale(gripOffset, scale);

        var t = handWeaponRenderer.transform;
        handWeaponRenderer.sprite = sprites[index];
        t.localPosition = (Vector3)fistsSwordHandOffset - gripFromPivot;
        t.localRotation = tilt;
        t.localScale = scale;
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
                PlayWeaponFireSound(weapon);
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

            // 맨손일 때는 손에 든 검도 몸통 애니메이션과 같이 휘두르기 시작한다.
            if (weapon == null)
            {
                fistsSwordSwinging = true;
                fistsSwordSwingStart = Time.time;
            }

            anim.Play(weapon != null ? weapon.attackAnimState : "AttackSlash", 0);
            ExecuteAttack(weapon);
            PlayWeaponFireSound(weapon);
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

        if (weapon.breakSfx != null && weaponAudioSource != null) StartCoroutine(PlayBreakSfxDelayed(weapon.breakSfx));

        EquipSlot(FistsSlot);
    }

    /// <summary>총기 브로크(빈 총) 사운드를 발사 사운드와 겹치지 않도록 0.5초 뒤에 재생한다.</summary>
    private IEnumerator PlayBreakSfxDelayed(AudioClip clip)
    {
        yield return new WaitForSeconds(0.5f);
        if (weaponAudioSource != null) weaponAudioSource.PlayOneShot(clip);
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

        SpawnMuzzleEffect(aimDir);

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

    // 현재 장착된 무기 비주얼의 Muzzle 자식이 없으면(맨손 등) 조용히 넘어간다.
    // 이펙트를 조준각으로 회전시키지 않고, 기본(왼쪽) 스프라이트를 오른쪽을 볼 때만 좌우 반전해서 재생한다.
    private void SpawnMuzzleEffect(Vector2 aimDir)
    {
        if (muzzleEffectPrefab == null || weaponMuzzle == null) return;

        GameObject effect = Instantiate(muzzleEffectPrefab, weaponMuzzle.position, Quaternion.identity);

        // 재생되는 짧은 시간 동안에도 플레이어가 움직이거나 총구가 흔들릴 수 있어 위치를 계속 따라가게 한다.
        var autoDestroy = effect.GetComponent<EffectAutoDestroy>();
        if (autoDestroy != null) autoDestroy.Follow(weaponMuzzle);

        var effectRenderer = effect.GetComponentInChildren<SpriteRenderer>();
        if (effectRenderer != null)
        {
            effectRenderer.flipX = aimDir.x > 0f;

            // 플레이어 전체(무기 포함)가 SortingGroup 하나로 그려지므로, 그 그룹의 레이어/순서보다
            // 한 칸 위로 맞춰야 무기 앞에 그려진다. weaponRenderer의 sortingLayerID는 그룹 안에서만
            // 의미가 있어(항상 Default) 그룹 밖의 이 이펙트에는 못 쓴다.
            if (sortingGroup != null)
            {
                effectRenderer.sortingLayerID = sortingGroup.sortingLayerID;
                effectRenderer.sortingOrder = sortingGroup.sortingOrder + 1;
            }
        }
    }

    /// <summary>
    /// 무기 발사/공격 사운드 1회 재생. Click()이 공격을 실행할 때마다(단발이든, 마우스를 누르고 있는
    /// 동안 쿨다운마다 반복 호출되는 연사/전기톱이든) 그때그때 호출되므로, 누르고 있는 동안 계속
    /// 눌러대는 무기(전기톱 등)는 자연스럽게 소리가 반복 재생된다. weapon이 null이면 맨손 공격이라
    /// fistsAttackSfx를 대신 재생한다.
    /// </summary>
    private void PlayWeaponFireSound(GameWeaponData weapon)
    {
        if (weaponAudioSource == null) return;

        AudioClip clip = weapon != null ? weapon.sfx : fistsAttackSfx;
        if (clip == null) return;

        weaponAudioSource.PlayOneShot(clip);
    }

    public void Hit(float dmg)
    {
        if (health <= 0f) return;
        health -= dmg;

        hitVignette?.PlayHitFlash();

        if (health <= 0f)
        {
            anim.Play("Death", 0);

            // StageManager를 거쳐야 좀비 전멸/시간초과와 동일하게 게임(스폰/AI/사운드)이 멈춘다.
            // 여기서 SpawnScoreCanvas()를 직접 부르면 StageManager.IsGameOver가 켜지지 않아
            // 몬스터가 죽은 뒤에도 계속 공격/스킬 사운드를 내는 문제가 생긴다.
            if (StageManager.Instance != null)
                StageManager.Instance.NotifyPlayerDied();
            else
                SpawnScoreCanvas();
        }
        else anim.Play("Hit", 0);
    }

    // 구급상자(MedicalPickup)가 획득 시 호출. 죽은 상태에서는 회복시키지 않는다.
    public void Heal(float amount)
    {
        if (health <= 0f) return;
        health = Mathf.Min(health + amount, maxHealth);
    }

    // StageManager가 몬스터를 전부 잡았을 때도 호출하므로 public으로 연다.
    public void SpawnScoreCanvas()
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
    public float GetStamina() { return stamina; }
    public float GetMaxStamina() { return staminaMax; }

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
