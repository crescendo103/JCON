using UnityEngine;

/// <summary>가장 가까운 적(들)에게 유도 투사체를 발사하는 시작 무기.</summary>
public class MagicMissileWeapon : WeaponBase
{
    protected override void Fire()
    {
        if (data.projectilePrefab == null || PoolManager.Instance == null) return;

        int count = Mathf.Max(1, Stats.projectileCount);
        var targets = EnemyTracker.FindNearestMultiple(OwnerTransform.position, count);
        if (targets.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var target = targets[i % targets.Count];
            var go = PoolManager.Instance.Get(data.projectilePrefab, OwnerTransform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            Vector2 dir = (Vector2)target.transform.position - (Vector2)OwnerTransform.position;
            proj.Launch(dir, ComputeDamage(), 6f, Stats.pierce, 3f, target.transform);
        }
    }
}
