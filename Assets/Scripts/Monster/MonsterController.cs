using UnityEngine;

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