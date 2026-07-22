using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rigid;

    [SerializeField] private float speed = 1f;
    private Vector2 input;

#if UNITY_EDITOR
    private void Reset()
    {
        rigid = this.GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
    }
#endif

    private void Update()
    {
        GetInput();
        Move();
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.A)) input.x = -1f;
        else if (Input.GetKey(KeyCode.D)) input.x = 1f;
        else input.x = 0f;

        if (Input.GetKey(KeyCode.W)) input.y = 1f;
        else if (Input.GetKey(KeyCode.S)) input.y = -1f;
        else input.y = 0f;
    }

    private void Move()
    {
        rigid.linearVelocity = input.normalized * speed;
    }

}
