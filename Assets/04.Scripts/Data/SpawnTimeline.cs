using System.Collections.Generic;
using UnityEngine;

/// <summary>시간 구간에 따라 등장 가능해지는 일반 적 항목.</summary>
[System.Serializable]
public class SpawnEntry
{
    public EnemyData enemyData;
    [Tooltip("이 시간(초) 이후부터 스폰 후보에 포함된다.")]
    public float activeFromSeconds = 0f;
    public float weight = 1f;
}

/// <summary>특정 시각에 발생하는 특수 스폰 이벤트 (엘리트 집단, 보스 등).</summary>
[System.Serializable]
public class MilestoneEvent
{
    public string label;
    public float triggerTimeSeconds;
    public EnemyData enemyData;
    public int count = 1;
}

[CreateAssetMenu(fileName = "SpawnTimeline", menuName = "VampireSurvivor/Spawn Timeline")]
public class SpawnTimeline : ScriptableObject
{
    public List<SpawnEntry> enemyEntries = new List<SpawnEntry>();
    public List<MilestoneEvent> milestones = new List<MilestoneEvent>();

    [Tooltip("시간(초) -> 스폰 간격(초). 값이 작을수록 자주 스폰.")]
    public AnimationCurve spawnIntervalOverTime = AnimationCurve.Linear(0f, 2f, 1200f, 0.15f);

    [Tooltip("시간(초) -> 적 체력/데미지 배율.")]
    public AnimationCurve statMultiplierOverTime = AnimationCurve.Linear(0f, 1f, 1200f, 3.1f);

    public int maxActiveEnemies = 180;
}
