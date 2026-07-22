using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private float speed = 5f;

    private InputSystem_Actions controls;
    private Vector2 input;

#if UNITY_EDITOR
    private void Reset()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        rigid.freezeRotation = true;
    }
#endif

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void OnDestroy()
    {
        controls.Dispose();
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void GetInput()
    {
        input = controls.Player.Move.ReadValue<Vector2>();
    }

    private void Move()
    {
        rigid.linearVelocity = input * speed;
    }
}
