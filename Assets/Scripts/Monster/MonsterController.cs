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

    // Animator의 Attack 상태 이름(전 몬스터 컨트롤러가 공통으로 이렇게 만듦). 공격 중 이동을 막는 데 쓴다.
    private const string AttackStateName = "Attack";

    public MonsterData data;
    protected Animator animator;
    private MonsterHealth health;
    public Transform target;

    // 마지막으로 이동했던 방향(정지 상태에서도 유지) → 공격 Blend Tree(FaceX/FaceY)가 재사용
    private Vector2 lastFacingDir = Vector2.down;

    // 넉백이 진행 중이면 MoveTowards가 위치를 덮어쓰지 않도록 막는다.
    private bool isKnockedBack;
    // 무적 시간 동안은 TakeDamage를 무시한다. BossController가 Hit 애니메이션 종료 시점을
    // 여기 맞추기 위해 오버라이드하므로 protected.
    protected bool isInvincible;
    // 사망 처리가 시작되면 AI/피격을 더 이상 진행하지 않는다.
    private bool isDead;

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
        if (isDead) return; // 사망 후에는 AI/이동/입력 처리를 모두 멈춘다.

        // 타겟이 없으면 다시 찾아보기 (씬 시작 순서 문제로 못 찾았을 경우 대비)
        if (target == null)
        {
            FindPlayer();
        }

        HandleSkillTestInput();

        if (data != null && data.aiBehavior != null)
        {
            data.aiBehavior.Execute(this);
        }
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
        if (IsPlayingState(AttackStateName)) return; // 공격 애니메이션 재생 중에는 제자리에서 공격만 한다.

        float speed = data != null ? data.speed : 1f;
        Vector3 currentPos = transform.position;
        Vector2 dir = ((Vector2)destination - (Vector2)currentPos).normalized;

        Vector2 movedXY = Vector2.MoveTowards(currentPos, destination, speed * Time.deltaTime);
        transform.position = new Vector3(movedXY.x, movedXY.y, currentPos.z);

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
    // 스킬의 effectPrefab을 타겟 위치에 스폰해 그 오브젝트에서 attackAnimation/파티클/사운드를 함께 실행한다.
    // 쿨타임(skill.cooldown) 동안은 재사용할 수 없다.
    // aimOverride를 넘기면 GetSkillTravelTarget() 대신 그 지점을 향해 이펙트가 날아간다
    // (예: RangedKiterAI가 계산한 예측 조준 지점).
    public void TriggerSkill(SkillData skill, Vector3? aimOverride = null)
    {
        if (skill == null) return;

        // 쿨타임 진행 중이면 스킬을 내보내지 않는다.
        if (skillsOnCooldown.Contains(skill)) return;

        TriggerAttack();                      // 본체 Attack 포즈
        SpawnSkillEffect(skill, aimOverride);  // 스킬 프리팹 + 애니메이션 + sfx

        if (skill.cooldown > 0f)
            StartCoroutine(SkillCooldownRoutine(skill));
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

    // 사거리 안일 때 AI가 호출. 주 스킬(skills[0])이 준비됐으면 스킬을,
    // 쿨타임 중/스킬 없음이면 일반 공격을 낸다.
    // aimOverride를 넘기면 스킬 이펙트가 타겟의 현재 위치 대신 그 지점을 향해 날아간다.
    public void AttackTarget(Vector3? aimOverride = null)
    {
        SkillData skill = (data != null && data.skills != null && data.skills.Length > 0)
            ? data.skills[0]
            : null;

        if (IsSkillReady(skill))
            TriggerSkill(skill, aimOverride);   // Attack + 스킬 애니메이션 동시, 쿨타임 시작
        else
            TriggerAttack();                     // 쿨타임 중 → 일반 공격만
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
    // 몬스터 정면에서 스폰해 타겟 방향(또는 aimOverride 지점)으로 총알처럼 날아가며, 애니메이션 클립
    // 길이(한 사이클) 동안 이동을 마치고 파괴된다. 애니메이션이 없으면 skill.effectDuration을 폴백
    // 이동 시간으로 사용한다. sfx는 effectPrefab 유무와 무관하게 스폰 위치에서 재생한다.
    // 이동/파괴는 SkillEffectMover(이펙트 자신)에게 맡긴다 — 여기(몬스터)의 코루틴으로 두면 몬스터가
    // 도중에 죽어 Die()의 StopAllCoroutines()에 걸릴 때 이펙트가 고아로 남기 때문.
    private void SpawnSkillEffect(SkillData skill, Vector3? aimOverride = null)
    {
        Vector3 spawnPos = GetSkillSpawnPosition();
        Vector3 travelTarget = aimOverride ?? GetSkillTravelTarget(spawnPos);

        if (skill.effectPrefab != null)
        {
            GameObject effect = Instantiate(skill.effectPrefab, spawnPos, Quaternion.identity);
            effect.transform.localScale *= skill.effectScale;

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
    public virtual void TriggerHit()
    {
        if (animator != null) animator.SetBool(ParamHit, true);
    }

    public void TriggerDeath()
    {
        if (animator != null) animator.SetTrigger(ParamDeath);
    }

    // ── 피격/넉백/무적시간 ──────────────────────────
    // 외부(플레이어 공격, 스킬 이펙트 등)에서 몬스터에게 데미지를 줄 때 호출한다.
    // sourcePosition은 공격이 날아온 위치로, 넉백 방향(공격 반대쪽)을 계산하는 데 쓰인다.
    // 무적 시간 중에는 완전히 무시한다(HP 변화, 넉백, Hit 트리거 모두 없음).
    public void TakeDamage(int amount, DamageType damageType, Vector2 sourcePosition)
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

        KnockbackSetting setting = GetKnockbackSetting(damageType);
        StartCoroutine(KnockbackRoutine(knockDir, setting));
        StartCoroutine(InvincibilityRoutine(setting.invincibilityDuration));
    }

    // 사망 처리: 진행 중이던 넉백/무적 코루틴을 멈춰 제자리에 고정한 뒤 Death 애니메이션을 재생하고,
    // 그 클립 길이만큼 기다렸다가 오브젝트를 파괴한다.
    private void Die()
    {
        isDead = true;

        StopAllCoroutines();
        isKnockedBack = false;
        Stop();
        TriggerDeath();

        float deathDuration = (data != null && data.animations != null && data.animations.death != null)
            ? data.animations.death.length
            : 1f;

        Destroy(gameObject, deathDuration);
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

    // dir 방향으로 setting.force 만큼 setting.duration에 걸쳐 밀려난다. Rigidbody2D가 Kinematic이므로
    // 물리 힘(AddForce) 대신 MoveTowards와 동일하게 transform을 직접 보간해서 옮긴다.
    private IEnumerator KnockbackRoutine(Vector2 dir, KnockbackSetting setting)
    {
        isKnockedBack = true;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(dir * setting.force);
        float elapsed = 0f;

        while (elapsed < setting.duration)
        {
            elapsed += Time.deltaTime;
            float t = setting.duration > 0f ? Mathf.Clamp01(elapsed / setting.duration) : 1f;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
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
}