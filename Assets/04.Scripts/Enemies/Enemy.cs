using UnityEngine;

/// <summary>기본 적: 단순 추격(플레이어 방향 직진) + 접촉 데미지. 경로탐색 없음.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    EnemyData data;
    float currentHealth;
    float statMultiplier = 1f;
    float contactTimer;

    Vector2 knockbackVelocity;
    float knockbackTimer;

    Rigidbody2D rb;
    SpriteRenderer sr;

    public bool IsAlive => currentHealth > 0f;
    public EnemyData Data => data;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        EnemyTracker.Register(this);
    }

    void OnDisable()
    {
        EnemyTracker.Unregister(this);
    }

    /// <summary>풀에서 꺼내질 때마다 호출되어 상태를 초기화한다.</summary>
    public void Initialize(EnemyData enemyData, float scaleMultiplier)
    {
        data = enemyData;
        statMultiplier = scaleMultiplier;
        currentHealth = data.baseHealth * scaleMultiplier;
        contactTimer = 0f;
        knockbackTimer = 0f;

        if (sr != null)
        {
            sr.sprite = data.sprite;
            sr.color = data.placeholderColor;
        }
        transform.localScale = Vector3.one * data.visualScale;
    }

    void Update()
    {
        if (contactTimer > 0f) contactTimer -= Time.deltaTime;
        if (knockbackTimer > 0f) knockbackTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return;
        if (GameManager.Instance.State != GameState.Playing) return;

        if (knockbackTimer > 0f)
        {
            rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            return;
        }

        Vector2 playerPos = GameManager.Instance.Player.transform.position;
        Vector2 dir = (playerPos - rb.position);
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            rb.MovePosition(rb.position + dir * data.moveSpeed * Time.fixedDeltaTime);
            if (sr != null) sr.flipX = dir.x < 0f;
        }
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        knockbackVelocity = dir * force;
        knockbackTimer = 0.15f;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (contactTimer > 0f) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(data.contactDamage * statMultiplier, transform.position);
            contactTimer = data.contactInterval;
        }
    }

    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (!IsAlive) return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        RunStats.Instance?.AddKill();
        PickupSpawner.Instance?.Spawn(data.gemGrade, transform.position);

        if (PoolManager.Instance != null) PoolManager.Instance.Return(gameObject);
        else Destroy(gameObject);
    }
}
