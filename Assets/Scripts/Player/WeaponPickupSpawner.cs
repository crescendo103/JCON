using UnityEngine;

// 몬스터용 StageManager(화면 밖 링 스폰)와는 별개의 단순한 무기 픽업 스포너.
// 초기 일괄 스폰 없이, spawnInterval(15초) 주기로 플레이어 주변(spawnRadius)에
// 랜덤 무기 1개씩 계속 스폰한다. initialSpawnCount를 0보다 크게 설정하면
// 씬 시작 직후 화면 안(initialSpawnRadius)에도 즉시 스폰할 수 있다.
public class WeaponPickupSpawner : MonoBehaviour
{
    public GameWeaponData[] possibleWeapons;
    public GameObject pickupPrefab;
    public float spawnInterval = 15f;
    public float spawnRadius = 8f;
    public float initialSpawnRadius = 3f;
    public int initialSpawnCount = 0;

    private float timer;

    private void Start()
    {
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
        if (possibleWeapons == null || possibleWeapons.Length == 0 || pickupPrefab == null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var weapon = possibleWeapons[Random.Range(0, possibleWeapons.Length)];
        Vector2 pos = (Vector2)player.transform.position + Random.insideUnitCircle * radius;

        var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
        var pickup = go.GetComponent<WeaponPickup>();
        if (pickup != null) pickup.Setup(weapon);
    }
}
