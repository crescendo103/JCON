using UnityEngine;

// 플레이어를 따라다니는 카메라. 2D 게임이므로 X/Y만 목표 위치를 따라가고
// Z는 카메라 원래 값(깊이)을 그대로 유지한다. SmoothDamp로 부드럽게 뒤쫓아간다.
public class GameCameraFollow : MonoBehaviour
{
    [Header("추적 대상 (비워두면 \"Player\" 태그로 자동 탐색)")]
    public Transform target;

    [Header("추적 방식")]
    [Tooltip("값이 작을수록 카메라가 더 빠르게(딱 붙어서) 따라간다")]
    public float smoothTime = 0.15f;
    public Vector2 offset = Vector2.zero;

    private Vector3 velocity = Vector3.zero;
    private float fixedZ;

    private void Awake()
    {
        fixedZ = transform.position.z;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
