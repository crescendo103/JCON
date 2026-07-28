using UnityEngine;

/// <summary>
/// 3x3 배경 타일을 카메라(또는 플레이어) 위치에 맞춰 타일 크기 단위로 스냅 이동시켜
/// 무한히 이어지는 배경처럼 보이게 한다. 실제로는 9장의 스프라이트만 재사용한다.
/// </summary>
public class InfiniteBackground : MonoBehaviour
{
    public Transform target;
    public float tileSize = 13f;

    void LateUpdate()
    {
        if (target == null) return;

        float x = Mathf.Round(target.position.x / tileSize) * tileSize;
        float y = Mathf.Round(target.position.y / tileSize) * tileSize;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
