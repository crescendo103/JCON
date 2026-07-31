using UnityEngine;
using UnityEngine.Tilemaps;

// 건물 콜라이더가 바닥(하단) 부분에만 있어서, 플레이어가 건물 스프라이트 뒤편(위쪽)으로
// 들어갈 수 있다. 그때 건물이 그대로 그려지면 플레이어가 화면에서 사라진 것처럼 보이므로,
// 플레이어가 이 타일맵의 렌더링 영역 안에 있는 동안 알파값을 낮춰 뒤에 있는 플레이어가 비쳐 보이게 한다.
[RequireComponent(typeof(Tilemap))]
public class BuildingSeeThrough : MonoBehaviour
{
    [Tooltip("플레이어가 건물 영역 안에 있을 때 적용할 알파값")]
    [SerializeField] private float occludedAlpha = 0.2f;
    [Tooltip("알파값이 목표치로 변하는 속도")]
    [SerializeField] private float fadeSpeed = 6f;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private Transform player;
    private float currentAlpha = 1f;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;
            player = playerObj.transform;
        }

        Bounds bounds = tilemapRenderer.bounds;
        Vector2 p = player.position;
        bool playerBehind = p.x >= bounds.min.x && p.x <= bounds.max.x
                          && p.y >= bounds.min.y && p.y <= bounds.max.y;

        float targetAlpha = playerBehind ? occludedAlpha : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        Color color = tilemap.color;
        if (!Mathf.Approximately(color.a, currentAlpha))
            tilemap.color = new Color(color.r, color.g, color.b, currentAlpha);
    }
}
