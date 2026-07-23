using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용 플레이어. WASD 이동 + 마우스 좌클릭 방향으로 히트스캔 사격해 몬스터에게 데미지를 준다.
// 실제 게임용 플레이어 시스템이 아니라 MonsterController.TakeDamage 연동을 확인하기 위한 임시 스크립트.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("사격")]
    public int shootDamage = 10;
    public DamageType shootDamageType = DamageType.Normal;
    public float shootRange = 20f;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;

        // 몬스터 스킬 이펙트(SkillEffectDamage)의 OnTriggerEnter2D가 감지하려면
        // 최소 한쪽에 Rigidbody2D가 있어야 하므로 Kinematic으로 붙여둔다.
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    void Update()
    {
        HandleMove();
        HandleShoot();
    }

    private void HandleMove()
    {
        if (Keyboard.current == null) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current[Key.W].isPressed) input.y += 1f;
        if (Keyboard.current[Key.S].isPressed) input.y -= 1f;
        if (Keyboard.current[Key.A].isPressed) input.x -= 1f;
        if (Keyboard.current[Key.D].isPressed) input.x += 1f;

        transform.position += (Vector3)(input.normalized * moveSpeed * Time.deltaTime);
    }

    private void HandleShoot()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 origin = transform.position;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = (mouseWorld - origin).normalized;

        Debug.DrawLine(origin, origin + dir * shootRange, Color.red, 0.3f);

        // Raycast는 자기 자신의 콜라이더 안에서 시작하므로 자기 자신을 맞고 끝나버린다.
        // RaycastAll로 받아서 자기 자신을 건너뛰고 가장 가까운 대상을 찾는다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, shootRange);
        RaycastHit2D? closest = null;
        foreach (var h in hits)
        {
            if (h.collider.gameObject == gameObject) continue;
            if (closest == null || h.distance < closest.Value.distance) closest = h;
        }
        if (closest == null) return;

        var monster = closest.Value.collider.GetComponent<MonsterController>();
        if (monster != null)
        {
            monster.TakeDamage(shootDamage, shootDamageType, origin);
        }
    }
}
