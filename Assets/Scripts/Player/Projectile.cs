using UnityEngine;

// 플레이어 사격에서 발사되는 총알. 지정한 방향으로 날아가다가 몬스터와 부딪히면 데미지를 주고 사라진다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float maxDistance = 20f;

    private int damage;
    private DamageType damageType;
    private Vector2 direction = Vector2.right;
    private Vector3 startPosition;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Launch(Vector2 dir, int dmg, DamageType type)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        damageType = type;
        startPosition = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var monster = other.GetComponent<MonsterController>();
        if (monster == null) return;

        monster.TakeDamage(damage, damageType, transform.position);
        Destroy(gameObject);
    }
}
