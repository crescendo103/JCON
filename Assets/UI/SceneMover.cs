using UnityEngine;

public class SceneMover : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("왼쪽으로 이동하는 속도")]
    public float moveSpeed = 2f;

    [Header("리셋 설정")]
    [Tooltip("이 대상보다 이만큼(미터) 왼쪽으로 벗어나면 리셋")]
    public Transform resetTarget;
    [Tooltip("resetTarget 기준 왼쪽으로 벗어나는 최대 거리")]
    public float leftMargin = 20f;

    void Update()
    {
        // 왼쪽으로 이동
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // resetTarget 위치보다 왼쪽으로 leftMargin 이상 벗어났는지 체크
        if (resetTarget.position.x - transform.position.x >= leftMargin)
        {
            // resetTarget의 현재 위치로 순간이동
            transform.position = resetTarget.position;
        }
    }
}