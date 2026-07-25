using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 시야(카메라 화면) 밖에서 몬스터를 주기적으로 자동 스폰하는 매니저.
/// 스폰 후보는 가중치(weight)를 가지고 있어서, 가중치가 클수록 더 자주 등장한다.
/// </summary>
public class StageManager : MonoBehaviour
{
    /// <summary>스폰 후보 하나 = 프리팹 + 등장 가중치.</summary>
    [System.Serializable]
    public class SpawnEntry
    {
        public string label;                 // 인스펙터에서 구분하기 위한 이름표 (선택)
        public GameObject prefab;            // 스폰할 몬스터 프리팹
        [Min(0f)] public float weight = 1f;  // 가중치가 클수록 자주 등장 (0이면 안 나옴)
    }

    public static StageManager Instance { get; private set; }

    [Header("스폰 후보 (프리팹 + 가중치)")]
    public SpawnEntry[] spawnTable;

    [Header("스폰 타이밍")]
    [Tooltip("몬스터를 스폰하는 주기(초)")]
    public float spawnInterval = 5f;
    [Tooltip("한 번에 스폰할 몬스터 수")]
    [Min(1)] public int spawnPerTick = 1;
    [Tooltip("동시에 존재할 수 있는 최대 몬스터 수")]
    public int maxAliveMonsters = 20;

    [Header("스폰 위치 (카메라 화면 밖 링)")]
    [Tooltip("화면 경계로부터 최소로 떨어뜨릴 여유 거리")]
    public float ringPadding = 1f;
    [Tooltip("ringPadding 이후 추가로 랜덤하게 더 벌어질 수 있는 두께")]
    public float ringThickness = 3f;

    [Header("참조 (비워두면 자동으로 찾음)")]
    [Tooltip("비워두면 Camera.main 사용")]
    public Camera targetCamera;
    [Tooltip("비워두면 \"Player\" 태그로 자동 탐색")]
    public Transform player;

    // 지금까지 스폰해서 살아있는(파괴되지 않은) 몬스터 목록
    private readonly List<GameObject> aliveMonsters = new List<GameObject>();
    private float spawnTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;
        TrySpawn();
    }

    /// <summary>스폰 조건(카메라/최대 마릿수)을 확인하고 spawnPerTick 만큼 스폰을 시도한다.</summary>
    private void TrySpawn()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
                return; // 카메라가 없으면 화면 밖 위치를 계산할 수 없음
        }

        // 죽어서 파괴된(null) 몬스터를 목록에서 정리
        aliveMonsters.RemoveAll(m => m == null);

        for (int i = 0; i < spawnPerTick; i++)
        {
            if (aliveMonsters.Count >= maxAliveMonsters)
                break;

            SpawnOne();
        }
    }

    /// <summary>가중치에 따라 몬스터 하나를 골라 화면 밖 랜덤 위치에 스폰한다.</summary>
    private void SpawnOne()
    {
        GameObject prefab = PickWeightedPrefab();
        if (prefab == null)
            return;

        Vector3 spawnPos = GetOffScreenPosition();
        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        aliveMonsters.Add(spawned);
    }

    /// <summary>spawnTable의 weight를 이용한 룰렛 방식 랜덤 선택.</summary>
    private GameObject PickWeightedPrefab()
    {
        if (spawnTable == null || spawnTable.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (SpawnEntry entry in spawnTable)
        {
            if (entry != null && entry.prefab != null)
                totalWeight += Mathf.Max(0f, entry.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        float accumulated = 0f;
        foreach (SpawnEntry entry in spawnTable)
        {
            if (entry == null || entry.prefab == null)
                continue;

            accumulated += Mathf.Max(0f, entry.weight);
            if (roll <= accumulated)
                return entry.prefab;
        }

        return null; // 이론상 도달하지 않음
    }

    /// <summary>
    /// 몬스터가 스폰되는 2D 평면(z=0) 위에서, 카메라 화면 사각형 바로 바깥
    /// ringPadding ~ ringPadding+ringThickness 범위의 랜덤한 각도 위치를 계산한다.
    /// ViewportToWorldPoint를 사용하므로 Orthographic/Perspective 카메라 모두에서 정확하다.
    /// (orthographicSize/aspect로 직접 계산하면 Perspective 카메라에서는 화면 크기가 완전히 틀어진다.)
    /// </summary>
    private Vector3 GetOffScreenPosition()
    {
        const float planeZ = 0f;
        float distance = Mathf.Abs(planeZ - targetCamera.transform.position.z);

        Vector3 center = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
        Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));

        float halfWidth = Mathf.Abs(topRight.x - center.x);
        float halfHeight = Mathf.Abs(topRight.y - center.y);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // 화면 사각형 경계까지의 거리(dir 방향으로) 계산
        float tx = dir.x != 0f ? halfWidth / Mathf.Abs(dir.x) : Mathf.Infinity;
        float ty = dir.y != 0f ? halfHeight / Mathf.Abs(dir.y) : Mathf.Infinity;
        float distToEdge = Mathf.Min(tx, ty);

        float extra = ringPadding + Random.Range(0f, ringThickness);
        float totalDist = distToEdge + extra;

        Vector3 offset = new Vector3(dir.x, dir.y, 0f) * totalDist;
        Vector3 result = (Vector3)(Vector2)center + offset;
        result.z = planeZ; // 2D 게임이므로 z는 고정

        return result;
    }

#if UNITY_EDITOR
    /// <summary>씬 뷰에서 스폰 링 범위를 시각적으로 확인하기 위한 기즈모.</summary>
    private void OnDrawGizmosSelected()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return;

        float distance = Mathf.Abs(0f - cam.transform.position.z);
        Vector3 center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distance));
        float halfWidth = Mathf.Abs(topRight.x - center.x);
        float halfHeight = Mathf.Abs(topRight.y - center.y);

        // 화면 경계
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));

        // 스폰 가능 바깥 범위(대략적인 사각형 기준)
        Gizmos.color = Color.red;
        float outerW = halfWidth + ringPadding + ringThickness;
        float outerH = halfHeight + ringPadding + ringThickness;
        Gizmos.DrawWireCube(center, new Vector3(outerW * 2f, outerH * 2f, 0f));
    }
#endif
}
