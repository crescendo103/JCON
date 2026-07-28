using UnityEngine;

/// <summary>플레이어가 바라보는(마지막 이동) 방향으로 근접 부채꼴(박스) 판정을 가하는 무기. 고레벨에서 양방향 타격.</summary>
public class WhipWeapon : WeaponBase
{
    protected override void Fire()
    {
        Vector2 facing = GameManager.Instance.Player.FacingDirection;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector2.right;

        HitInDirection(facing);

        if (Stats.projectileCount >= 2)
        {
            HitInDirection(-facing);
        }
    }

    void HitInDirection(Vector2 dir)
    {
        float area = Stats.area * ComputeAreaMultiplier();
        Vector2 origin = OwnerTransform.position;
        Vector2 center = origin + dir * (0.7f * area);
        Vector2 size = new Vector2(1.4f * area, 1.0f * area);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var hits = Physics2D.OverlapBoxAll(center, size, angle);
        float dmg = ComputeDamage();
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(dmg, center);
            }
        }
    }
}
