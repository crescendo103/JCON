using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public MonsterData data;
    private int currentHP;
    private Animator animator;
    public Transform target;

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
    public void MoveTowards(Vector3 destination)
    {
        float speed = data != null ? data.speed : 1f;
        Vector3 dir = (destination - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("MoveX", dir.x);
            animator.SetFloat("MoveY", dir.y);
        }
    }

    public void Stop()
    {
        if (animator != null)
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }
    }

    public float DistanceToTarget()
    {
        if (target == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, target.position);
    }
}