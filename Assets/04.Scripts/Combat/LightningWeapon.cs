using UnityEngine;

/// <summary>화면 내 랜덤 적(들)에게 즉발 낙뢰 데미지를 준다. 투사체 이동 없이 즉시 적용.</summary>
public class LightningWeapon : WeaponBase
{
    protected override void Fire()
    {
        int count = Mathf.Max(1, Stats.projectileCount);
        var targets = EnemyTracker.FindRandomMultiple(count);

        foreach (var t in targets)
        {
            if (t == null || !t.IsAlive) continue;

            t.TakeDamage(ComputeDamage(), t.transform.position);

            if (data.projectilePrefab != null && PoolManager.Instance != null)
            {
                var vfx = PoolManager.Instance.Get(data.projectilePrefab, t.transform.position, Quaternion.identity);
                var timed = vfx.GetComponent<TimedReturn>();
                if (timed != null) timed.Activate(0.2f);
            }
        }
    }
}
