using UnityEngine;

/// <summary>플레이어를 부드럽게 추적하는 단순 카메라. Cinemachine 없이 직접 구현.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Tooltip("클수록 더 빠르게(더 뻣뻣하게) 따라간다.")]
    public float followSharpness = 12f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // SmoothDamp는 스프링처럼 목표를 오버슈트(지나쳤다가 되돌아옴)할 수 있어
        // 조이스틱처럼 방향이 자주 바뀌는 입력에서는 흔들림으로 보인다.
        // 지수 감쇠(exponential decay) 방식은 절대 오버슈트하지 않아 더 안정적이다.
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
