using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용 플레이어. WASD 이동 + 마우스 좌클릭 방향으로 총알(bulletPrefab)을 발사해 몬스터에게 데미지를 준다.
// 실제 게임용 플레이어 시스템이 아니라 MonsterController.TakeDamage 연동을 확인하기 위한 임시 스크립트.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("사격")]
    public GameObject bulletPrefab;
    public int shootDamage = 10;
    public DamageType shootDamageType = DamageType.Normal;
    public float bulletSpeed = 15f;
    public float shootRange = 20f;

    // 원거리 몬스터 AI(RangedKiterAI)가 조준을 예측(리드샷)할 때 참고하는 현재 이동 속도.
    public Vector2 CurrentVelocity { get; private set; }

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
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current[Key.W].isPressed) input.y += 1f;
            if (Keyboard.current[Key.S].isPressed) input.y -= 1f;
            if (Keyboard.current[Key.A].isPressed) input.x -= 1f;
            if (Keyboard.current[Key.D].isPressed) input.x += 1f;
        }

        Vector2 velocity = input.normalized * moveSpeed;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        CurrentVelocity = velocity;
    }

    private void HandleShoot()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (cam == null) cam = Camera.main;
        if (cam == null || bulletPrefab == null) return;

        Vector2 origin = transform.position;
        Vector2 dir = GetAimDirection(origin);

        GameObject bulletObj = Instantiate(bulletPrefab, origin, Quaternion.identity);
        Projectile bullet = bulletObj.GetComponent<Projectile>();
        if (bullet == null) bullet = bulletObj.AddComponent<Projectile>();

        bullet.speed = bulletSpeed;
        bullet.maxDistance = shootRange;
        bullet.Launch(dir, shootDamage, shootDamageType);
    }

    // 마우스 스크린 좌표를 게임 오브젝트와 같은 Z 평면(z = origin.z) 위의 월드 좌표로 변환한다.
    // 카메라가 Orthographic이 아니라 Perspective인 경우 ScreenToWorldPoint(screenPos)에 z값을 주지 않으면
    // (기본값 0) 결과가 항상 카메라 위치 근처로 계산되어 마우스 위치와 무관한 방향이 나온다.
    // 이를 피하기 위해 카메라 종류와 무관하게 동작하는 평면 레이캐스트 방식을 사용한다.
    private Vector2 GetAimDirection(Vector2 origin)
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorld = ray.GetPoint(enter);
            return ((Vector2)mouseWorld - origin).normalized;
        }

        return Vector2.right;
    }
}
