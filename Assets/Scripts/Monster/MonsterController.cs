using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// 피격 시 넉백 세기/무적시간을 다르게 적용하기 위한 데미지 종류.
public enum DamageType { Light, Normal, Heavy }

public class MonsterController : MonoBehaviour
{
    // Monster Maker가 생성하는 AnimatorController와 이름을 맞춘 파라미터 상수.
    public const string ParamMoveX = "MoveX";
    public const string ParamMoveY = "MoveY";
    public const string ParamFaceX = "FaceX";
    public const string ParamFaceY = "FaceY";
    public const string ParamSpeed = "Speed";
    public const string ParamAttack = "Attack";
    public const string ParamHit = "Hit";
    public const string ParamDeath = "Death";

    // Animator의 Attack/Hit 상태 이름(전 몬스터 컨트롤러가 공통으로 이렇게 만듦).
    // Attack은 공격 중 이동을 막는 데, Hit은 재생 중 재시작을 막는 데 쓴다.
    private const string AttackStateName = "Attack";
    private const string HitStateName = "Hit";

    public MonsterData data;
    protected Animator animator;
    private MonsterHealth health;
    private Rigidbody2D rb;
    // 몬스터 본체 스프라이트. 피격 파티클을 이 뒤로 보내는 sortingOrder 기준으로 쓴다(SpawnHitEffect 참고).
    private SpriteRenderer bodyRenderer;
    public Transform target;

    // MoveTowards가 계산한 이번 프레임 이동 속도. 실제 적용은 FixedUpdate에서 rb에 대입한다
    // (transform을 직접 옮기면 Rigidbody2D/Collider2D 물리 충돌을 그냥 통과해버리기 때문).
    private Vector2 desiredVelocity;

    [Header("피격 이펙트")]
    [Tooltip("피격 시 이 중 하나를 무작위로 재생한다. 런타임에 Instantiate하는 게 아니라, 이 몬스터 " +
             "프리팹 밑에 미리 자식으로 붙여둔 파티클 이펙트를 그대로 재사용한다. Scaling Mode가 " +
             "Local인 파티클(MasterMagicFX 등)은 부모 스케일을 무시하고 자기 로컬 스케일만 보기 때문에, " +
             "런타임에 스케일을 보정하는 대신 에디터에서 자식으로 둔 채 눈으로 보면서 크기/위치를 맞추는 " +
             "쪽이 훨씬 간단하고 정확하다. 각 자식은 평소 비활성 상태로 있다가 PlayEffect에서 재생된다")]
    [SerializeField] private GameObject[] hitEffectPrefabs;

    [Header("최초 교전 딜레이")]
    [Tooltip("플레이어가 공격 사거리 안에 처음 들어온 순간부터 첫 공격이 나가기까지 대기하는 시간(초). " +
             "사거리에 막 들어오자마자 곧바로 맞는 것을 막아 플레이어에게 반응할 시간을 준다. " +
             "몬스터 인스턴스 생애에 딱 한 번만 적용되며(이후 사거리를 들락날락해도 다시 걸리지 않음), " +
             "스킬 자체의 선딜(SkillData.windupTime)과는 별개로 그 앞에 추가된다. 0이면 기존처럼 즉시 공격한다.")]
    [SerializeField] private float firstEngageDelay = 1f;

    [Header("근접 Idle 사운드")]
    [Tooltip("플레이어가 이 거리 안에 있는 동안 idleSoundInterval마다 반복 재생되는 사운드. 비워두면 아무 일도 하지 않는다. " +
             "실제 클립은 인스펙터에서 직접 연결한다")]
    [SerializeField] private AudioClip idleSfx;
    [Tooltip("idleSfx가 재생되는 거리(월드 유닛)")]
    [SerializeField] private float idleSoundRange = 5f;
    [Tooltip("idleSfx를 반복 재생하는 간격(초). 이 쿨타임은 플레이어가 사거리 안에 있는지와 무관하게 " +
             "항상 흘러간다 — 그래야 플레이어가 사거리 경계에서 왔다갔다해도 그때마다 리셋되어 다시 " +
             "재생되는 게 아니라, 마지막 재생 이후 정말 이 시간이 지나야만(그리고 그 순간 사거리 안이어야) 재생된다")]
    [SerializeField] private float idleSoundInterval = 5f;

    private AudioSource idleAudioSource;
    // 다음 idle 사운드 재생까지 남은 시간. 사거리 진입/이탈과 무관하게 매 프레임 줄어든다.
    private float idleSoundCooldown;

    // 마지막으로 이동했던 방향(정지 상태에서도 유지) → 공격 Blend Tree(FaceX/FaceY)가 재사용
    private Vector2 lastFacingDir = Vector2.down;

    // 넉백이 진행 중이면 MoveTowards가 위치를 덮어쓰지 않도록 막는다.
    private bool isKnockedBack;
    // 무적 시간 동안은 TakeDamage를 무시한다. BossController가 Hit 애니메이션 종료 시점을
    // 여기 맞추기 위해 오버라이드하므로 protected.
    protected bool isInvincible;
    // 사망 처리가 시작되면 AI/피격을 더 이상 진행하지 않는다.
    private bool isDead;
    // 공격 선딜(SkillWindupRoutine)이 진행 중인지. Attack 애니메이션 상태는 클립 길이(약 0.1~0.2초)가
    // windupTime(예: 0.3초)보다 짧아 먼저 Idle로 돌아가 버리므로, MoveTowards를 막는 용도로는
    // IsPlayingState(AttackStateName)만으로 부족하다 — 이 플래그로 선딜 구간 전체를 감싼다.
    private bool isAttacking;

    // firstEngageDelay 대기가 끝났는지(=첫 공격을 이미 내보냈는지). 몬스터 인스턴스당 한 번만 쓰고
    // 그 뒤로는 계속 true로 남아, 사거리를 벗어났다 다시 들어와도 다시 걸리지 않는다.
    private bool firstEngageDelayElapsed;
    // FirstEngageDelayRoutine이 이미 시작됐는지(사거리 안에 있는 동안 매 프레임 AttackTarget이
    // 호출되므로, 코루틴이 중복으로 여러 개 시작되지 않도록 막는다).
    private bool firstEngageDelayStarted;

    // 현재 쿨타임이 진행 중인 스킬들. 코루틴이 채우고/비운다. (몬스터 인스턴스별 상태)
    private readonly HashSet<SkillData> skillsOnCooldown = new HashSet<SkillData>();

    // 임시 테스트용: 숫자 키(1~9)를 눌러 data.skills에 등록된 스킬을 순서대로 사용해본다.
    private static readonly Key[] SkillTestKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bodyRenderer = GetComponent<SpriteRenderer>();

        // 좀비 프리팹 어디에도 AudioSource가 없으므로(MonsterHealth와 동일한 자가 보강 패턴),
        // 프리팹을 손대지 않아도 idle 사운드가 재생될 수 있도록 없으면 여기서 추가한다.
        idleAudioSource = GetComponent<AudioSource>();
        if (idleAudioSource == null) idleAudioSource = gameObject.AddComponent<AudioSource>();
        idleAudioSource.playOnAwake = false;
    }

    void Start()
    {
        // 기존 프리팹을 수정하지 않아도 자동으로 체력 관리 컴포넌트가 붙도록 없으면 추가한다.
        health = GetComponent<MonsterHealth>();
        if (health == null) health = gameObject.AddComponent<MonsterHealth>();

        if (data != null)
            health.Initialize(data.maxHP);

        FindPlayer();
    }

    void Update()
    {
        if (isDead || StageManager.IsGameOver) return; // 사망 후, 또는 스테이지가 끝난 후에는 AI/이동/입력 처리를 모두 멈춘다.

        // 타겟이 없으면 다시 찾아보기 (씬 시작 순서 문제로 못 찾았을 경우 대비)
        if (target == null)
        {
            FindPlayer();
        }

        HandleSkillTestInput();
        UpdateIdleSound();

        // AI가 이번 프레임에 MoveTowards를 호출하지 않으면(공격 중, 사거리 안 등) 자동으로 멈춘다.
        desiredVelocity = Vector2.zero;

        if (data != null && data.aiBehavior != null)
        {
            data.aiBehavior.Execute(this);
        }
    }

    void FixedUpdate()
    {
        if (isDead || isKnockedBack || rb == null || StageManager.IsGameOver) return;
        rb.linearVelocity = desiredVelocity;
    }

    private void HandleSkillTestInput()
    {
        if (Keyboard.current == null || data == null || data.skills == null) return;

        for (int i = 0; i < data.skills.Length && i < SkillTestKeys.Length; i++)
        {
            if (Keyboard.current[SkillTestKeys[i]].wasPressedThisFrame)
            {
                TriggerSkill(data.skills[i]);
            }
        }
    }

    // ── 플레이어 찾기 ──────────────────────────
    private void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    // ── 이동 + 애니메이션 파라미터 갱신 ──────────────────────────
    // 2D 게임이므로 이동/거리 계산은 XY 평면만 사용한다(Z는 그대로 유지).
    // Z를 같이 계산하면 몬스터와 타겟의 Z가 어긋나 있을 때 거리 판정이 항상 커져
    // 사거리 안에 못 들어가는(=계속 이동만 하고 공격 전환이 안 되는) 문제가 생긴다.
    public void MoveTowards(Vector3 destination)
    {
        if (isKnockedBack) return; // 넉백 코루틴이 위치를 담당하는 동안은 AI 이동을 막는다.
        if (isInvincible) return; // 피격 후 무적 시간 동안은(넉백이 끝난 뒤에도) 제자리에 멈춰 있는다.
        if (isAttacking) return; // 선딜 중에는 제자리 고정 — 쫓아가며 때리면 lastFacingDir가 바뀌어
                                  // 이미 확정된 스폰 위치/방향과 어긋나고, 예고 동작 중 계속 다가오면 회피도 무의미해진다.
        if (IsPlayingState(AttackStateName)) return; // 공격 애니메이션 재생 중에는 제자리에서 공격만 한다.

        float speed = data != null ? data.speed : 1f;
        Vector3 currentPos = transform.position;
        Vector2 toDestination = (Vector2)destination - (Vector2)currentPos;
        Vector2 dir = toDestination.normalized;

        // 목적지에 거의 다 왔으면(한 물리 스텝 안에 도착하는 거리) 그만큼만 속도를 줄여
        // Rigidbody2D 기반 이동에서도 MoveTowards처럼 목적지를 지나치지 않게 한다.
        float step = speed * Time.fixedDeltaTime;
        float moveSpeed = toDestination.magnitude < step ? toDestination.magnitude / Time.fixedDeltaTime : speed;
        desiredVelocity = dir * moveSpeed;

        if (dir.sqrMagnitude > 0.0001f)
        {
            lastFacingDir = dir;
        }

        if (animator != null)
        {
            animator.SetFloat(ParamMoveX, dir.x);
            animator.SetFloat(ParamMoveY, dir.y);
            animator.SetFloat(ParamFaceX, lastFacingDir.x);
            animator.SetFloat(ParamFaceY, lastFacingDir.y);
            animator.SetFloat(ParamSpeed, speed);
        }
    }

    // animator가 현재 stateName 상태를 재생 중인지 확인한다(레이어 0 기준).
    private bool IsPlayingState(string stateName)
    {
        return animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    public void Stop()
    {
        desiredVelocity = Vector2.zero;
        // AI는 공격 사거리 안에 들어오면 매 프레임 Stop()을 호출한다. 넉백 중에 여기서 velocity를 0으로
        // 만들면 KnockbackRoutine이 걸어둔 속도가 바로 다음 프레임에 지워져 넉백이 사실상 사라진다
        // (MoveTowards/FixedUpdate에는 이미 같은 가드가 있음 — 근접 사거리 안 몬스터에 한해 빠졌던 것).
        if (rb != null && !isKnockedBack) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat(ParamMoveX, 0);
            animator.SetFloat(ParamMoveY, 0);
            animator.SetFloat(ParamSpeed, 0);
        }
    }

    // ── 공격/피격/사망 트리거 ──────────────────────────
    // AI/전투 로직에서 호출. FaceX/FaceY는 이동 방향을 그대로 재사용하므로 여기서는 트리거만 발동.
    public void TriggerAttack()
    {
        if (animator != null) animator.SetTrigger(ParamAttack);
    }

    // SkillData 기반 공격. 몬스터 본체는 기존과 동일하게 공용 Attack 포즈만 재생하고,
    // skill.windupTime만큼 기다린 뒤 스킬의 effectPrefab을 스폰해 그 오브젝트에서
    // attackAnimation/파티클/사운드를 함께 실행한다. 쿨타임(skill.cooldown) 동안은 재사용할 수 없다.
    // aimOverride를 넘기면 GetSkillTravelTarget() 대신 그 지점을 향해 이펙트가 날아간다
    // (예: RangedKiterAI가 계산한 예측 조준 지점).
    public void TriggerSkill(SkillData skill, Vector3? aimOverride = null)
    {
        if (skill == null || isDead) return;

        // 이미 선딜/공격이 진행 중이면 겹쳐 내지 않는다(스킬이 여러 개인 몬스터가 서로 다른
        // 스킬을 동시에 쏘는 것을 방지 — 예: 숫자키 테스트 입력이 겹쳐 눌렸을 때).
        if (isAttacking) return;

        // 쿨타임 진행 중이면 스킬을 내보내지 않는다.
        if (skillsOnCooldown.Contains(skill)) return;

        // 쿨타임은 선딜이 "시작"하는 이 시점부터 돌린다. 선딜이 끝난 뒤 시작하면 공격 간격이
        // windupTime만큼 그대로 늘어나 의도한 쿨타임보다 훨씬 느려지기 때문.
        if (skill.cooldown > 0f)
            StartCoroutine(SkillCooldownRoutine(skill));

        StartCoroutine(SkillWindupRoutine(skill, aimOverride));
    }

    // 공격 모션을 먼저 재생하고 skill.windupTime만큼 기다린 뒤에 데미지 이펙트를 스폰한다.
    // 조준 지점은 대기 "전"에 확정한다 — 선딜 동안 플레이어가 자리를 뜨면 이펙트가 빈 자리로
    // 날아가게 되므로, 공격 모션이 실제로 회피 가능한 예고 신호로 기능하게 된다.
    private IEnumerator SkillWindupRoutine(SkillData skill, Vector3? aimOverride)
    {
        // isAttacking은 Attack 애니메이션 상태 자체보다 오래 켜져 있어야 한다 — 공격 클립은
        // windupTime보다 훨씬 짧게(약 0.1~0.2초) Idle로 돌아가므로, MoveTowards를 막는 기준을
        // 애니메이션 상태가 아니라 이 플래그로 잡아야 선딜 내내 제자리에 고정된다.
        isAttacking = true;
        TriggerAttack(); // 본체 Attack 포즈 = 플레이어에게 보내는 예고

        Vector3 travelTarget = aimOverride ?? GetSkillTravelTarget(GetSkillSpawnPosition());

        if (skill.windupTime > 0f)
            yield return new WaitForSeconds(skill.windupTime);

        // 선딜 도중 몬스터가 죽으면 Die()의 StopAllCoroutines()에 걸려 애초에 여기 도달하지
        // 않지만, 프레임 경계 문제 등을 대비해 방어적으로 한 번 더 확인한다.
        // StageManager.IsGameOver도 함께 확인 — 게임 오버로 Time.timeScale이 멎기 직전/직후
        // 경계 프레임에 뒤늦게 판정이 나가 플레이어가 이미 끝난 뒤에 맞는 것을 막는다.
        if (!isDead && !StageManager.IsGameOver)
            SpawnSkillEffect(skill, travelTarget);

        isAttacking = false;
    }

    // skill.cooldown 초 동안 해당 스킬을 사용 불가 상태로 둔다.
    private IEnumerator SkillCooldownRoutine(SkillData skill)
    {
        skillsOnCooldown.Add(skill);
        yield return new WaitForSeconds(skill.cooldown);
        skillsOnCooldown.Remove(skill);
    }

    public bool IsSkillReady(SkillData skill)
    {
        return skill != null && !skillsOnCooldown.Contains(skill);
    }

    // 사거리 안일 때 AI가 매 프레임 호출. 주 스킬(skills[0])이 준비됐으면 스킬을 낸다.
    // 쿨타임 중이면 아무것도 하지 않는다 — 예전처럼 쿨타임 중에도 매 프레임 Attack 포즈를
    // 다시 걸면 공격 모션이 상시 재생돼, 선딜(예고) 동작과 구분이 안 돼 회피가 무의미해진다.
    // aimOverride를 넘기면 스킬 이펙트가 타겟의 현재 위치 대신 그 지점을 향해 날아간다.
    public void AttackTarget(Vector3? aimOverride = null)
    {
        if (isAttacking) return; // 선딜/공격 진행 중 → 이번 프레임엔 아무 것도 하지 않는다.

        // 첫 교전 딜레이: 사거리 안에 막 들어온 순간 곧바로 때리지 않고 firstEngageDelay만큼 대기한다.
        // AI가 이미 Stop()을 호출한 뒤 이 메서드를 부르므로, 대기 중에는 자연히 제자리에 멈춰 있는다.
        if (!firstEngageDelayElapsed)
        {
            if (!firstEngageDelayStarted)
                StartCoroutine(FirstEngageDelayRoutine());
            return;
        }

        SkillData skill = (data != null && data.skills != null && data.skills.Length > 0)
            ? data.skills[0]
            : null;

        // 스킬이 없는 몬스터는 낼 수 있는 게 포즈밖에 없으므로 기존 동작을 유지한다.
        if (skill == null) { TriggerAttack(); return; }

        if (IsSkillReady(skill))
            TriggerSkill(skill, aimOverride);
    }

    // 사거리 안에 처음 들어온 뒤 firstEngageDelay만큼 기다렸다가 첫 공격을 허용한다.
    // 이 코루틴은 몬스터 인스턴스 생애에 딱 한 번만 실행된다(firstEngageDelayStarted가 막는다).
    private IEnumerator FirstEngageDelayRoutine()
    {
        firstEngageDelayStarted = true;

        if (firstEngageDelay > 0f)
            yield return new WaitForSeconds(firstEngageDelay);

        firstEngageDelayElapsed = true;
    }

    // 몬스터 정면(마지막 이동/공격 방향)으로 스킬 이펙트를 띄우는 거리.
    private const float SkillSpawnDistance = 1f;

    // 타겟이 없을 때 이펙트가 날아갈 거리(총알처럼 정면으로 날아감).
    private const float SkillNoTargetTravelDistance = 4f;

    // 스킬 이펙트 스폰 위치: 항상 몬스터 정면(마지막 이동 방향) 기준으로 스폰한다.
    // 여기서 타겟 쪽으로(총알처럼) 날아가는 건 SpawnSkillEffect의 이동 코루틴이 처리한다.
    private Vector3 GetSkillSpawnPosition()
    {
        return transform.position + (Vector3)lastFacingDir * SkillSpawnDistance;
    }

    // 이펙트가 날아가서 도착할 지점: 타겟이 있으면 타겟 위치, 없으면 스폰 위치에서 정면으로 더 나아간 지점.
    private Vector3 GetSkillTravelTarget(Vector3 spawnPos)
    {
        if (target != null) return target.position;
        return spawnPos + (Vector3)lastFacingDir * SkillNoTargetTravelDistance;
    }

    // skill.effectPrefab을 스폰한다. 연출(애니메이션, 콜라이더 등)은 Effect Prefab Maker로 만든
    // 프리팹 자체가 이미 갖추고 있으므로, 여기서는 SkillData의 값(피해량/방향/크기)만 그 위에 덮어써 준다.
    // 몬스터 정면에서 스폰해 travelTarget(선딜이 시작될 때 이미 확정된 지점) 쪽으로 총알처럼 날아가며,
    // 애니메이션 클립 길이(한 사이클) 동안 이동을 마치고 파괴된다. 애니메이션이 없으면 skill.effectDuration을
    // 폴백 이동 시간으로 사용한다. sfx는 effectPrefab 유무와 무관하게 스폰 위치에서 재생한다.
    // 이동/파괴는 SkillEffectMover(이펙트 자신)에게 맡긴다 — 여기(몬스터)의 코루틴으로 두면 몬스터가
    // 도중에 죽어 Die()의 StopAllCoroutines()에 걸릴 때 이펙트가 고아로 남기 때문.
    // spawnPos는 여기서(선딜이 끝난 시점의 몬스터 위치 기준으로) 새로 계산한다 — 선딜 동안 넉백 등으로
    // 몬스터가 밀렸을 수 있으므로 windup 시작 시점 값을 그대로 쓰지 않는다.
    private void SpawnSkillEffect(SkillData skill, Vector3 travelTarget)
    {
        Vector3 spawnPos = GetSkillSpawnPosition();

        if (skill.effectPrefab != null)
        {
            GameObject effect = Instantiate(skill.effectPrefab, spawnPos, Quaternion.identity);
            effect.transform.localScale *= skill.effectScale;
            ApplyHitboxScale(effect, skill);

            var dmg = effect.GetComponent<SkillEffectDamage>();
            if (dmg == null) dmg = effect.AddComponent<SkillEffectDamage>();
            dmg.damage = skill.damage;

            Vector2 travelDir = ((Vector2)travelTarget - (Vector2)spawnPos).sqrMagnitude > 0.0001f
                ? ((Vector2)travelTarget - (Vector2)spawnPos).normalized
                : lastFacingDir;
            effect.GetComponent<SkillEffectFacing>()?.SetFacing(travelDir);

            float duration = GetEffectCycleDuration(effect, skill.effectDuration);
            var mover = effect.AddComponent<SkillEffectMover>();
            mover.Launch(spawnPos, travelTarget, duration);
        }

        if (skill.sfx != null)
        {
            AudioSource.PlayClipAtPoint(skill.sfx, spawnPos);
        }
    }

    // effectScale은 그림과 콜라이더를 함께 키운다. hitboxScale이 지정돼 있으면 그 차이만큼
    // 콜라이더 크기를 되돌려, 이펙트는 크게 보이되 실제 피격 판정만 좁게 만든다.
    // Collider2D의 radius/size는 로컬 좌표 값이라 localScale에 곱해진 뒤이므로, 여기서 비율만
    // 되돌려주면 그림(스프라이트/파티클)에는 영향을 주지 않는다.
    private static void ApplyHitboxScale(GameObject effect, SkillData skill)
    {
        if (skill.hitboxScale <= 0f || skill.effectScale <= 0f) return;
        if (Mathf.Approximately(skill.hitboxScale, skill.effectScale)) return;

        float ratio = skill.hitboxScale / skill.effectScale;

        foreach (Collider2D col in effect.GetComponentsInChildren<Collider2D>())
        {
            if (col is CircleCollider2D circle) circle.radius *= ratio;
            else if (col is BoxCollider2D box) box.size *= ratio;
            else if (col is CapsuleCollider2D caps) caps.size *= ratio;
        }
    }

    // 피격 방향(dir)으로 hitEffectPrefabs 중 하나를 무작위로 골라 재생한다. 이 배열의 각 원소는
    // 몬스터 프리팹 밑에 미리 자식으로 붙여둔(에디터에서 직접 배치·스케일 조정한) 이펙트 오브젝트다.
    // 매번 Instantiate하지 않으므로 GC 할당도 없고, Local 스케일링 모드인 파티클(MasterMagicFX 등)의
    // 부모-스케일-무시 문제도 애초에 발생하지 않는다 — 에디터에서 자식으로 둔 채 눈으로 보면서
    // 스케일/위치를 맞췄기 때문이다(런타임에 스케일을 보정해줄 필요가 없다).
    private void SpawnHitEffect(Vector2 dir)
    {
        if (hitEffectPrefabs == null || hitEffectPrefabs.Length == 0) return;

        GameObject effect = hitEffectPrefabs[Random.Range(0, hitEffectPrefabs.Length)];
        if (effect == null) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        effect.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        SendEffectBehindBody(effect);
        PlayEffect(effect);
    }

    // 평소 비활성 상태로 있던 이펙트 자식을 켜고, 안에 있는(활성 상태인) 파티클 시스템들을 처음부터
    // 다시 재생한다. Clear로 이전 재생에서 남은 파티클을 먼저 지워야 연속으로 맞았을 때 이상하게
    // 겹쳐 보이지 않는다. includeInactive를 안 쓰는 이유는, 이펙트 내부에 원본 제작자가 일부러
    // 비활성으로 둔 하위 레이어가 있을 수 있어서(예: 다른 트리거로만 켜지게 설계된 레이어) —
    // 그런 레이어까지 강제로 재생시키지 않고 원래 설계된 활성 상태를 그대로 존중한다.
    private void PlayEffect(GameObject effect)
    {
        effect.SetActive(true);

        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    // effect 안의 모든 렌더러(스프라이트/파티클)를 몬스터 본체(bodyRenderer)와 같은 정렬 레이어에,
    // 그보다 한 단계 낮은 sortingOrder로 맞춰 몬스터 뒤에서 그려지게 한다. bodyRenderer가 없으면
    // (본체에 SpriteRenderer가 없는 특수 몬스터 등) 프리팹 원래 정렬 값을 그대로 둔다.
    private void SendEffectBehindBody(GameObject effect)
    {
        if (bodyRenderer == null) return;

        int layerID = bodyRenderer.sortingLayerID;
        int order = bodyRenderer.sortingOrder - 1;

        foreach (SpriteRenderer r in effect.GetComponentsInChildren<SpriteRenderer>())
        {
            r.sortingLayerID = layerID;
            r.sortingOrder = order;
        }

        foreach (ParticleSystemRenderer r in effect.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            r.sortingLayerID = layerID;
            r.sortingOrder = order;
        }
    }

    // effect의 Animator에 연결된 클립들 중 가장 긴 길이(한 사이클)를 이펙트 유지 시간으로 사용한다.
    // Animator나 클립이 없는 이펙트(사운드/파티클만 있는 경우 등)는 fallbackDuration을 그대로 쓴다.
    private float GetEffectCycleDuration(GameObject effect, float fallbackDuration)
    {
        var animator = effect.GetComponent<Animator>();
        var clips = animator != null ? animator.runtimeAnimatorController?.animationClips : null;

        if (clips == null || clips.Length == 0) return fallbackDuration;

        return clips.Max(clip => clip.length);
    }

    // Hit는 Trigger가 아니라 Bool로 다룬다 — 무적 시간이 끝나는 순간(InvincibilityRoutine)까지
    // Hit 애니메이션을 붙잡아 두었다가 그때 바로 다음 상태로 넘어가게 해서, 클립 자체 길이와
    // 무관하게 Hit 재생 시간이 항상 무적 시간과 정확히 일치하게 한다.
    // 이미 Hit 상태가 재생 중이면(무적시간이 애니메이션 길이보다 짧게 설정된 경우 등) 다시
    // SetBool(true)을 호출하지 않는다 — false→true로 값이 바뀌는 것 자체가 재시작을 유발하므로,
    // 현재 재생 중인 Hit이 끝까지 재생된 뒤에야 다음 피격이 새로 Hit을 시작할 수 있게 한다.
    public virtual void TriggerHit()
    {
        if (animator == null) return;
        if (IsPlayingState(HitStateName)) return;
        animator.SetBool(ParamHit, true);
    }

    public void TriggerDeath()
    {
        if (animator != null) animator.SetTrigger(ParamDeath);
    }

    // ── 피격/넉백/무적시간 ──────────────────────────
    // 외부(플레이어 공격, 스킬 이펙트 등)에서 몬스터에게 데미지를 줄 때 호출한다.
    // sourcePosition은 공격이 날아온 위치로, 넉백 방향(공격 반대쪽)을 계산하는 데 쓰인다.
    // knockbackDistanceOverride가 0보다 크면 무기(GameWeaponData)/맨손 쪽에서 지정한 거리로
    // 넉백 세기(force)를 덮어쓴다. 0 이하(기본값)면 몬스터 쪽 knockbackSettings를 그대로 쓴다.
    // 무적 시간 중에는 완전히 무시한다(HP 변화, 넉백, Hit 트리거 모두 없음).
    public void TakeDamage(int amount, DamageType damageType, Vector2 sourcePosition, float knockbackDistanceOverride = 0f)
    {
        if (isInvincible || isDead) return;

        bool died = health.ApplyDamage(amount);

        if (died)
        {
            Die();
            return; // 넉백 없이 제자리에서 사망 처리
        }

        TriggerHit();

        Vector2 knockDir = (Vector2)transform.position - sourcePosition;
        if (knockDir.sqrMagnitude < 0.0001f) knockDir = -lastFacingDir;
        knockDir.Normalize();

        SpawnHitEffect(knockDir);

        KnockbackSetting setting = GetKnockbackSetting(damageType);
        // setting.force가 0이면(예: KingZombie) 넉백 면역으로 의도한 것이므로 무기 값으로 덮어쓰지 않는다.
        if (knockbackDistanceOverride > 0f && setting.force > 0f)
        {
            // GetKnockbackSetting은 MonsterData(SO)가 소유한 인스턴스를 그대로 돌려줄 수 있어
            // setting.force를 직접 대입하면 .asset이 영구히 오염된다. 복사본을 새로 만들어 쓴다.
            setting = new KnockbackSetting
            {
                type = setting.type,
                force = knockbackDistanceOverride,
                duration = setting.duration,
                invincibilityDuration = setting.invincibilityDuration,
            };
        }

        StartCoroutine(KnockbackRoutine(knockDir, setting));
        StartCoroutine(InvincibilityRoutine(setting.invincibilityDuration));
    }

    // 사망 처리: 진행 중이던 넉백/무적 코루틴을 멈춰 제자리에 고정한 뒤 Death 애니메이션을 재생하고,
    // 그 재생 길이만큼 기다렸다가 오브젝트를 파괴한다.
    private void Die()
    {
        isDead = true;

        StopAllCoroutines();
        isKnockedBack = false;
        Stop();
        // Update()가 isDead에서 조기 반환해 더 이상 UpdateIdleSound()가 불리지 않으므로, 마침 재생
        // 중이던 idle 사운드를 여기서 직접 끊지 않으면 시체가 파괴될 때까지(DestroyAfterDeathAnimation) 들린다.
        if (idleAudioSource != null) idleAudioSource.Stop();
        // 사망 연출 동안 다른 몬스터/이펙트에 밀려 시체가 미끄러지지 않게 물리를 완전히 끈다.
        if (rb != null) rb.simulated = false;
        TriggerDeath();

        StartCoroutine(DestroyAfterDeathAnimation());
    }

    // Death는 8방향 Blend Tree일 수도 있어(Motion에는 AnimationClip.length 같은 고정 길이가 없음)
    // data.animations.death.length를 직접 읽는 대신, 실제로 Death 상태에 들어갈 때까지 기다린 뒤
    // AnimatorStateInfo.length(현재 블렌드 상태 기준 실제 재생 길이, 단일 클립/Blend Tree 모두 처리됨)로
    // 대기 시간을 구한다. AnyState -> Death 전이는 hasExitTime이라 즉시 바뀌지 않을 수 있어 대기가 필요하다.
    private IEnumerator DestroyAfterDeathAnimation()
    {
        if (animator != null)
        {
            const float timeout = 2f; // 안전장치: 무슨 이유로든 Death 상태에 못 들어가도 무한 대기하지 않음
            float waited = 0f;
            while (!IsPlayingState("Death") && waited < timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (IsPlayingState("Death"))
            {
                yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            }
        }

        Destroy(gameObject);
    }

    // data(MonsterData)에 등록된 넉백 설정에서 damageType에 맞는 항목을 찾고, 없으면 기본값을 반환한다.
    private KnockbackSetting GetKnockbackSetting(DamageType damageType)
    {
        if (data != null && data.knockbackSettings != null)
        {
            foreach (var setting in data.knockbackSettings)
            {
                if (setting.type == damageType) return setting;
            }
        }

        switch (damageType)
        {
            case DamageType.Light:
                return new KnockbackSetting { type = damageType, force = 1.5f, duration = 0.1f, invincibilityDuration = 0.3f };
            case DamageType.Heavy:
                return new KnockbackSetting { type = damageType, force = 6f, duration = 0.25f, invincibilityDuration = 1f };
            default:
                return new KnockbackSetting { type = damageType, force = 3f, duration = 0.15f, invincibilityDuration = 0.5f };
        }
    }

    // dir 방향으로 setting.force 만큼 setting.duration에 걸쳐 밀려난다. Rigidbody2D가 Dynamic이므로
    // transform을 직접 보간하는 대신 velocity를 걸어 물리 엔진이 벽 충돌을 함께 처리하게 한다
    // (그래야 넉백 도중 벽을 뚫고 나가지 않는다).
    private IEnumerator KnockbackRoutine(Vector2 dir, KnockbackSetting setting)
    {
        isKnockedBack = true;

        if (setting.duration > 0f)
        {
            if (rb != null) rb.linearVelocity = dir * (setting.force / setting.duration);
            yield return new WaitForSeconds(setting.duration);
        }
        else if (rb != null)
        {
            rb.position += dir * setting.force; // duration이 0이면 그 자리에서 즉시 이동
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    protected virtual IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        if (animator != null) animator.SetBool(ParamHit, false);
    }

    public float DistanceToTarget()
    {
        if (target == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, target.position);
    }

    // 플레이어가 idleSoundRange 안에 있는 동안 idleSoundInterval마다 idleSfx를 한 번씩 재생한다.
    private void UpdateIdleSound()
    {
        if (idleSfx == null || idleAudioSource == null) return;

        // 쿨타임은 사거리 진입/이탈과 무관하게 항상 줄어든다 — 사거리 경계에서 왔다갔다한다고
        // 매번 리셋되어 다시 재생되지 않고, 마지막 재생 이후 idleSoundInterval초가 실제로 지나야
        // (그리고 그 순간 사거리 안이어야) 재생된다.
        idleSoundCooldown -= Time.deltaTime;
        if (idleSoundCooldown > 0f) return;

        if (DistanceToTarget() > idleSoundRange) return;

        idleAudioSource.PlayOneShot(idleSfx);
        idleSoundCooldown = idleSoundInterval;
    }
}