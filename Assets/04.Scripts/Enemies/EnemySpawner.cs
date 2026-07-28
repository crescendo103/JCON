using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시간 기반 스폰 디렉터. SpawnTimeline 데이터를 참조해 일반 스폰/마일스톤 이벤트/리퍼 등장을 제어한다.
/// 모든 적은 하나의 범용 Enemy 프리팹을 재사용하고, EnemyData로 스탯/스프라이트만 다르게 초기화한다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public SpawnTimeline timeline;
    public GameObject enemyPrefab;
    public EnemyData reaperData;

    [Tooltip("플레이어 기준 스폰 링 거리 (카메라 화면 밖).")]
    public float spawnRingDistance = 9f;

    float spawnTimer;
    readonly HashSet<MilestoneEvent> firedMilestones = new HashSet<MilestoneEvent>();
    bool reaperSpawned;

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        if (GameManager.Instance.Player == null) return;

        float t = GameManager.Instance.ElapsedTime;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Mathf.Max(0.05f, timeline.spawnIntervalOverTime.Evaluate(t));
            TrySpawnRegular(t);
        }

        CheckMilestones(t);

        if (!reaperSpawned && t >= GameManager.ReaperSpawnTime)
        {
            SpawnReaper();
            reaperSpawned = true;
        }
    }

    void TrySpawnRegular(float t)
    {
        if (EnemyTracker.Active.Count >= timeline.maxActiveEnemies) return;

        SpawnEntry chosen = null;
        float totalWeight = 0f;
        foreach (var entry in timeline.enemyEntries)
        {
            if (entry.activeFromSeconds <= t) totalWeight += entry.weight;
        }
        if (totalWeight <= 0f) return;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var entry in timeline.enemyEntries)
        {
            if (entry.activeFromSeconds > t) continue;
            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                chosen = entry;
                break;
            }
        }
        if (chosen == null) return;

        float scale = timeline.statMultiplierOverTime.Evaluate(t);
        SpawnEnemyAt(chosen.enemyData, GetSpawnPositionAroundPlayer(), scale);
    }

    void CheckMilestones(float t)
    {
        foreach (var m in timeline.milestones)
        {
            if (firedMilestones.Contains(m)) continue;
            if (t < m.triggerTimeSeconds) continue;

            firedMilestones.Add(m);
            float scale = timeline.statMultiplierOverTime.Evaluate(t);
            for (int i = 0; i < m.count; i++)
            {
                SpawnEnemyAt(m.enemyData, GetSpawnPositionAroundPlayer(), scale);
            }
        }
    }

    void SpawnReaper()
    {
        if (reaperData == null) return;
        Vector2 pos = GetSpawnPositionAroundPlayer();
        SpawnEnemyAt(reaperData, pos, 1f);
    }

    void SpawnEnemyAt(EnemyData data, Vector2 position, float scale)
    {
        if (data == null || enemyPrefab == null) return;
        if (EnemyTracker.Active.Count >= timeline.maxActiveEnemies) return;

        var go = PoolManager.Instance.Get(enemyPrefab, position, Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        enemy.Initialize(data, scale);
    }

    Vector2 GetSpawnPositionAroundPlayer()
    {
        Vector2 center = GameManager.Instance.Player.transform.position;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRingDistance;
        return center + offset;
    }
}
