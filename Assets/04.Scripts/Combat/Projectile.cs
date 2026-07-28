using System.Collections.Generic;
using UnityEngine;

/// <summary>매직 미사일/투척 단검 등에서 공용으로 쓰는 이동형 투사체. 관통/유도를 지원한다.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    float damage;
    float speed;
    int pierceRemaining;
    float lifeTime;
    float timer;
    Vector2 direction;
    Transform homingTarget;

    readonly HashSet<Enemy> hitSet = new HashSet<Enemy>();

    public void Launch(Vector2 dir, float dmg, float spd, int pierce, float life, Transform homing = null)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        speed = spd;
        pierceRemaining = pierce;
        lifeTime = life;
        timer = 0f;
        homingTarget = homing;
        hitSet.Clear();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        if (homingTarget != null)
        {
            direction = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnOrDestroy();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || !enemy.IsAlive || hitSet.Contains(enemy)) return;

        enemy.TakeDamage(damage, transform.position);
        hitSet.Add(enemy);

        if (pierceRemaining <= 0)
        {
            ReturnOrDestroy();
        }
        else
        {
            pierceRemaining--;
        }
    }

    /// <summary>풀 매니저가 아직 준비되지 않았거나 씬 전환 도중인 경우를 대비한 안전한 반환.</summary>
    void ReturnOrDestroy()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
