using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        else if (target != null)
        {
            // AI가 없을 때 테스트용: 그냥 플레이어를 향해 이동
            MoveTowards(target.position);
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

    public float DistanceToTarget()
    {
        if (target == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, target.position);
    }
}