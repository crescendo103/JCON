using System.Collections.Generic;
using UnityEngine;

// 플레이어 사격에서 발사되는 총알. 지정한 방향으로 날아가다가 몬스터와 부딪히면 데미지를 주고 사라진다.
// pierce가 있으면(저격총 등) 이미 맞은 대상은 건너뛰고 남은 관통 횟수만큼 계속 날아간다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GameProjectile : MonoBehaviour
{
    public float speed = 15f;
    public float maxDistance = 20f;

    [Header("이펙트")]
    [Tooltip("벽/몬스터에 맞아 사라질 때 재생되는 이펙트. 애니메이션 한 바퀴 끝나면 EffectAutoDestroy가 알아서 없앤다")]
    public GameObject disappearEffectPrefab;

    private int damage;
    private DamageType damageType;
    private Vector2 direction = Vector2.right;
    private Vector3 startPosition;
    private int pierceRemaining;
    private int wallLayer;
    private float knockbackDistance;
    private readonly HashSet<MonsterController> hitMonsters = new HashSet<MonsterController>();

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        wallLayer = LayerMask.NameToLayer("WallGrid");
    }

    public void Launch(Vector2 dir, int dmg, DamageType type, int pierce = 0, float knockback = 0f)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        damageType = type;
        startPosition = transform.position;
        pierceRemaining = pierce;
        knockbackDistance = knockback;
        hitMonsters.Clear();

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
        if (other.gameObject.layer == wallLayer)
        {
            SpawnEffect(disappearEffectPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
            return;
        }

        var monster = other.GetComponent<MonsterController>();
        if (monster == null || hitMonsters.Contains(monster)) return;

        monster.TakeDamage(damage, damageType, transform.position, knockbackDistance);
        hitMonsters.Add(monster);

        if (pierceRemaining <= 0)
        {
            SpawnEffect(disappearEffectPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
        }
        else
        {
            pierceRemaining--;
        }
    }

    private void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;

        Instantiate(prefab, position, rotation);
    }
}
