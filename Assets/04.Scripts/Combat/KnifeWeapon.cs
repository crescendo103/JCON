using UnityEngine;

/// <summary>플레이어가 바라보는 방향으로 직선 관통 투사체를 발사.</summary>
public class KnifeWeapon : WeaponBase
{
    protected override void Fire()
    {
        if (data.projectilePrefab == null || PoolManager.Instance == null) return;

        Vector2 facing = GameManager.Instance.Player.FacingDirection;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector2.right;

        int count = Mathf.Max(1, Stats.projectileCount);
        float spreadStep = 8f;

        for (int i = 0; i < count; i++)
        {
            float angleOffset = (i - (count - 1) / 2f) * spreadStep;
            Vector2 dir = Quaternion.Euler(0f, 0f, angleOffset) * facing;

            var go = PoolManager.Instance.Get(data.projectilePrefab, OwnerTransform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            proj.Launch(dir, ComputeDamage(), 10f, Stats.pierce, 2.5f);
        }
    }
}
