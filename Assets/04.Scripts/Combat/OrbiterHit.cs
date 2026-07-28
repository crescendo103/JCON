using System.Collections.Generic;
using UnityEngine;

/// <summary>회전 성서(Bible)의 궤도 오브젝트에 부착. 접촉한 적에게 일정 간격으로 틱 데미지를 준다.</summary>
public class OrbiterHit : MonoBehaviour
{
    public BibleWeapon weapon;

    readonly Dictionary<Enemy, float> lastHitTime = new Dictionary<Enemy, float>();
    const float HitInterval = 0.3f;

    void OnTriggerStay2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || !enemy.IsAlive || weapon == null) return;

        if (lastHitTime.TryGetValue(enemy, out var t) && Time.time - t < HitInterval) return;

        enemy.TakeDamage(weapon.ComputeDamage(), transform.position);
        lastHitTime[enemy] = Time.time;
    }
}
