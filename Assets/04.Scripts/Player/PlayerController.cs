using UnityEngine;

/// <summary>플로팅 조이스틱 입력을 받아 플레이어를 이동시키고, 시작 무기/카메라/GameManager 등록을 처리한다.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] WeaponData startingWeaponData;


    // Player.controller의 Direction 파라미터: 0=Down, 1=Left, 2=Right, 3=Up
    const int DirDown = 0;
    const int DirLeft = 1;
    const int DirRight = 2;
    const int DirUp = 3;
    static readonly int DirectionParamHash = Animator.StringToHash("Direction");

    Rigidbody2D rb;
    Animator animator;
    Vector2 moveInput;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        var health = GetComponent<PlayerHealth>();
        var stats = GetComponent<PlayerStats>();
        var weapons = GetComponent<WeaponInventory>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this, health, stats, weapons);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.target = transform;
        }

        if (weapons != null && startingWeaponData != null)
        {
            weapons.AddWeapon(startingWeaponData);
        }
    }


    void Update()
    {
        moveInput = joystick != null ? joystick.InputVector : Vector2.zero;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        if (isMoving)
        {
            FacingDirection = moveInput.normalized;
            UpdateAnimatorDirection(moveInput);
        }

        // 이동 중이 아니면 애니메이터를 멈춰 마지막 걷기 프레임에서 정지(임시 아이들 처리).
        if (animator != null) animator.speed = isMoving ? 1f : 0f;
    }

    void UpdateAnimatorDirection(Vector2 input)
    {
        if (animator == null) return;

        int direction = Mathf.Abs(input.x) > Mathf.Abs(input.y)
            ? (input.x > 0f ? DirRight : DirLeft)
            : (input.y > 0f ? DirUp : DirDown);

        animator.SetInteger(DirectionParamHash, direction);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        var stats = GameManager.Instance != null ? GameManager.Instance.PlayerStats : null;
        float speed = stats != null ? stats.MoveSpeed : 4f;
        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
    }
}
