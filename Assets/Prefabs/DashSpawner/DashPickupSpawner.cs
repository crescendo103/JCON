using UnityEngine;

// WeaponPickupSpawner/MedicalPickupSpawner와 동일한 방식으로 동작하는 대쉬 픽업 스포너.
// 초기 일괄 스폰 없이, spawnInterval(15초) 주기로 플레이어 주변(spawnRadius)에
// 대쉬 픽업 1개씩 계속 스폰한다. initialSpawnCount를 0보다 크게 설정하면
// 씬 시작 직후 화면 안(initialSpawnRadius)에도 즉시 스폰할 수 있다.
public class DashPickupSpawner : MonoBehaviour
{
    public GameObject pickupPrefab;
    public float spawnInterval = 15f;
    public float spawnRadius = 8f;
    public float initialSpawnRadius = 3f;
    public int initialSpawnCount = 0;

    [Tooltip("비워두면 씬에서 자동으로 찾음. 벽/건물 위나 플레이어가 갈 수 없는 고립 구역에 스폰되는 것을 막는 데 사용")]
    public StageMapGenerator mapGenerator;
    [Tooltip("뽑은 위치가 벽/건물 위이거나 도달 불가 구역이면 다시 뽑는 최대 횟수")]
    [Min(1)] public int maxSpawnPositionAttempts = 10;

    private float timer;

    private void Start()
    {
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<StageMapGenerator>();

        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnRandom(initialSpawnRadius);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        SpawnRandom(spawnRadius);
    }

    private void SpawnRandom(float radius)
    {
        if (pickupPrefab == null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (!TryGetSpawnPosition(player.transform.position, radius, out Vector2 pos)) return;

        Instantiate(pickupPrefab, pos, Quaternion.identity);
    }

    /// <summary>
    /// mapGenerator가 있으면 플레이어 주변 랜덤 위치가 ground 타일(벽/건물 아님) 중 플레이어가
    /// 실제로 걸어서 갈 수 있는 구역인지 검사해서 걸러낸다. maxSpawnPositionAttempts번 안에 유효한
    /// 자리를 못 찾으면 실패로 처리한다. mapGenerator가 없으면 기존처럼 검사 없이 랜덤 위치를 반환한다.
    /// </summary>
    private bool TryGetSpawnPosition(Vector3 playerPos, float radius, out Vector2 pos)
    {
        if (mapGenerator == null)
        {
            pos = (Vector2)playerPos + Random.insideUnitCircle * radius;
            return true;
        }

        mapGenerator.ComputeReachability(playerPos);

        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            Vector2 candidate = (Vector2)playerPos + Random.insideUnitCircle * radius;
            if (mapGenerator.IsWorldPositionSpawnable(candidate))
            {
                pos = candidate;
                return true;
            }
        }

        pos = default;
        return false;
    }
}
