using UnityEngine;

// 필드에 스폰되는 무기 픽업 아이템. 플레이어가 트리거로 닿으면 무기를 지급하고 사라진다.
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public GameWeaponData weapon;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("스케일과 무관하게 유지할 픽업 판정 반경(월드 기준)")]
    [SerializeField] private float pickupRadius = 0.4f;

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
        ApplyVisual();
    }

    // 스포너가 Instantiate 직후 무기를 지정할 때 사용. Awake는 Instantiate 도중 이미 실행되므로
    // Instantiate 이후에 weapon을 대입하는 것만으로는 스프라이트가 갱신되지 않는다.
    public void Setup(GameWeaponData w)
    {
        weapon = w;
        ApplyVisual();
    }

    // 장착 시(displayScale)와 동일한 크기로 필드에 표시한다. 스프라이트가 없는 무기는
    // WeaponVisuals의 임시 도형으로 대체 표시한다.
    private void ApplyVisual()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (weapon == null)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        Sprite sprite;
        Color color;
        float scale;
        WeaponVisuals.Resolve(weapon.pickupSprite, weapon.displayScale, out sprite, out color, out scale);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;
        }

        scale = Mathf.Max(0.01f, scale);
        transform.localScale = Vector3.one * scale;

        // localScale이 콜라이더에도 곱해지므로, 스케일과 무관하게 판정 반경이 pickupRadius로
        // 유지되도록 역보정한다.
        var circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null) circleCol.radius = pickupRadius / scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponent<GamePlayerController>();
        if (pc != null && weapon != null)
        {
            pc.PickupWeapon(weapon);
            Destroy(gameObject);
        }
    }
}
