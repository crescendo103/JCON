using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어 주위를 도는 궤도 오브젝트. 상시 지속형이라 쿨다운 대신 자체 Update로 회전을 처리한다.</summary>
public class BibleWeapon : WeaponBase
{
    readonly List<Transform> orbiters = new List<Transform>();
    float angleOffset;

    protected override void Fire()
    {
        // 상시 지속형 무기라 쿨다운 발동은 사용하지 않음.
    }

    protected override void OnInitialize() => RebuildOrbiters();
    protected override void OnLevelChanged() => RebuildOrbiters();

    void RebuildOrbiters()
    {
        for (int i = orbiters.Count - 1; i >= 0; i--)
        {
            if (orbiters[i] != null) Destroy(orbiters[i].gameObject);
        }
        orbiters.Clear();

        int count = Mathf.Max(1, Stats.projectileCount);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("BibleOrbiter");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon;
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var hit = go.AddComponent<OrbiterHit>();
            hit.weapon = this;

            orbiters.Add(go.transform);
        }
    }

    protected override void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        if (orbiters.Count == 0) return;

        float rotSpeed = Stats.extra; // deg/sec
        angleOffset += rotSpeed * Time.deltaTime;

        float radius = Stats.area * ComputeAreaMultiplier();
        int count = orbiters.Count;
        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            orbiters[i].localPosition = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
        }
    }
}
