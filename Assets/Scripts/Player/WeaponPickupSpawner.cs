using UnityEngine;

// 몬스터용 StageManager(화면 밖 링 스폰)와는 별개의 단순한 무기 픽업 스포너.
// 씬 시작 직후 화면 안(initialSpawnRadius)에 하나 즉시 스폰해 곧바로 슬롯을 채울 수 있게 하고,
// 이후에는 spawnInterval/spawnRadius 주기로 플레이어 주변에 계속 스폰한다.
public class WeaponPickupSpawner : MonoBehaviour
{
    public WeaponData[] possibleWeapons;
    public GameObject pickupPrefab;
    public float spawnInterval = 15f;
    public float spawnRadius = 8f;
    public float initialSpawnRadius = 3f;

    private float timer;

    private void Start()
    {
        SpawnRandom(initialSpawnRadius);
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
        if (pickup != null) pickup.weapon = weapon;
    }
}
