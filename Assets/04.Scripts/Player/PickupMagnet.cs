using UnityEngine;

/// <summary>플레이어 픽업(자석) 반경. CircleCollider2D(Trigger)를 PlayerStats.PickupRadius에 맞춰 매 프레임 갱신한다.</summary>
[RequireComponent(typeof(CircleCollider2D))]
public class PickupMagnet : MonoBehaviour
{
    [SerializeField] PlayerStats stats;
    CircleCollider2D trig;

    void Awake()
    {
        trig = GetComponent<CircleCollider2D>();
        trig.isTrigger = true;
        if (stats == null) stats = GetComponentInParent<PlayerStats>();
    }

    void Update()
    {
        if (stats != null) trig.radius = stats.PickupRadius;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var gem = other.GetComponent<XPGem>();
        if (gem != null) gem.StartAttracting(transform);
    }
}
