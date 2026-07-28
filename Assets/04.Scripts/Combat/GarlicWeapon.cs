using UnityEngine;

/// <summary>플레이어 중심 원형 오라. 상시 지속형으로 일정 간격마다 범위 내 적에게 틱 데미지 + 약한 넉백을 준다.</summary>
public class GarlicWeapon : WeaponBase
{
    float tickTimer;
    const float TickInterval = 0.2f;
    const float KnockbackForce = 2f;

    protected override void Fire()
    {
        // 상시 지속형 무기라 쿨다운 발동은 사용하지 않음.
    }

    protected override void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = TickInterval;

        float radius = Stats.area * ComputeAreaMultiplier();
        Vector2 origin = OwnerTransform.position;
        float dmgPerTick = ComputeDamage() * TickInterval;

        var hits = Physics2D.OverlapCircleAll(origin, radius);
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Enemy>();
            if (enemy == null || !enemy.IsAlive) continue;

            enemy.TakeDamage(dmgPerTick, origin);

            Vector2 away = ((Vector2)enemy.transform.position - origin);
            if (away.sqrMagnitude > 0.0001f)
            {
                enemy.ApplyKnockback(away.normalized, KnockbackForce);
            }
        }
    }
}
