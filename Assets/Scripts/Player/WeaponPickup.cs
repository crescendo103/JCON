using UnityEngine;

// 필드에 스폰되는 무기 픽업 아이템. 플레이어가 트리거로 닿으면 무기를 지급하고 사라진다.
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponData weapon;
    [SerializeField] private SpriteRenderer spriteRenderer;

#if UNITY_EDITOR
    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
#endif

    private void Awake()
    {
        if (weapon != null && spriteRenderer != null) spriteRenderer.sprite = weapon.pickupSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponent<PlayerController>();
        if (pc != null && weapon != null)
        {
            pc.PickupWeapon(weapon);
            Destroy(gameObject);
        }
    }
}
