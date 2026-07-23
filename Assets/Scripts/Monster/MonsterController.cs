using System.Collections;
using System.Collections.Generic;
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

    public MonsterData data;
    private int currentHP;
    private Animator animator;
    public Transform target;

    // 마지막으로 이동했던 방향(정지 상태에서도 유지) → 공격 Blend Tree(FaceX/FaceY)가 재사용
    private Vector2 lastFacingDir = Vector2.down;

    // 넉백이 진행 중이면 MoveTowards가 위치를 덮어쓰지 않도록 막는다.
    private bool isKnockedBack;
    // 무적 시간 동안은 TakeDamage를 무시한다.
    private bool isInvincible;

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
        if (data != null)
            currentHP = data.maxHP;

        FindPlayer();
    }

    void Update()
    {
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
    public void TriggerSkill(SkillData skill)
    {
        if (skill == null) return;

        // 쿨타임 진행 중이면 스킬을 내보내지 않는다.
        if (skillsOnCooldown.Contains(skill)) return;

        TriggerAttack();          // 본체 Attack 포즈
        SpawnSkillEffect(skill);  // 스킬 프리팹 + 애니메이션 + sfx

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
    public void AttackTarget()
    {
        SkillData skill = (data != null && data.skills != null && data.skills.Length > 0)
            ? data.skills[0]
            : null;

        if (IsSkillReady(skill))
            TriggerSkill(skill);   // Attack + 스킬 애니메이션 동시, 쿨타임 시작
        else
            TriggerAttack();       // 쿨타임 중 → 일반 공격만
    }

    // 스킬 이펙트 스폰 위치: 타겟(공격 대상)이 있으면 그 위치, 없으면 몬스터 정면(마지막 이동 방향).
    private Vector3 GetSkillSpawnPosition()
    {
        if (target != null) return target.position;
        return transform.position + (Vector3)lastFacingDir;
    }

    // skill.effectPrefab을 스폰한다. 연출(애니메이션, 콜라이더 등)은 Effect Prefab Maker로 만든
    // 프리팹 자체가 이미 갖추고 있으므로, 여기서는 SkillData의 값(피해량/방향)만 그 위에 덮어써 준다.
    // effectDuration이 지나면 파괴하고, sfx는 effectPrefab 유무와 무관하게 스폰 위치에서 재생한다.
    private void SpawnSkillEffect(SkillData skill)
    {
        Vector3 spawnPos = GetSkillSpawnPosition();

        if (skill.effectPrefab != null)
        {
            GameObject effect = Instantiate(skill.effectPrefab, spawnPos, Quaternion.identity);

            var dmg = effect.GetComponent<SkillEffectDamage>();
            if (dmg == null) dmg = effect.AddComponent<SkillEffectDamage>();
            dmg.damage = skill.damage;

            effect.GetComponent<SkillEffectFacing>()?.SetFacing(lastFacingDir);

            Destroy(effect, skill.effectDuration);
        }

        if (skill.sfx != null)
        {
            AudioSource.PlayClipAtPoint(skill.sfx, spawnPos);
        }
    }

    public void TriggerHit()
    {
        if (animator != null) animator.SetTrigger(ParamHit);
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
        if (isInvincible) return;

        currentHP -= amount;
        TriggerHit();

        Vector2 knockDir = (Vector2)transform.position - sourcePosition;
        if (knockDir.sqrMagnitude < 0.0001f) knockDir = -lastFacingDir;
        knockDir.Normalize();

        KnockbackSetting setting = GetKnockbackSetting(damageType);
        StartCoroutine(KnockbackRoutine(knockDir, setting));
        StartCoroutine(InvincibilityRoutine(setting.invincibilityDuration));

        if (currentHP <= 0)
        {
            TriggerDeath();
        }
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

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    public float DistanceToTarget()
    {
        if (target == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, target.position);
    }
}