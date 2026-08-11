using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 임의의 위치에 몬스터를 주기적으로 자동 스폰하는 매니저. 벽/건물 위나 플레이어가 갈 수 없는
/// 고립 구역에는 스폰되지 않는다(mapGenerator 참조 필요). mapGenerator가 없으면 카메라 화면 밖 링에서 스폰한다.
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
    public int maxAliveMonsters = 3;

    [Header("스테이지 목표")]
    [Tooltip("이번 스테이지에서 총 스폰할 몬스터 수. 이 수만큼 스폰하고 나면 더 이상 스폰하지 않는다")]
    public int totalMonstersToSpawn = 10;

    [Tooltip("스테이지가 1 오를 때마다 totalMonstersToSpawn에 더해지는 마릿수")]
    public int monstersPerStageIncrease = 3;

    [Header("점수")]
    [Tooltip("좀비 한 마리를 잡을 때마다 올라가는 점수")]
    public int scorePerKill = 100;

    [Header("스폰 위치 (mapGenerator 없을 때만 사용하는 카메라 화면 밖 링 폴백)")]
    [Tooltip("화면 경계로부터 최소로 떨어뜨릴 여유 거리")]
    public float ringPadding = 1f;
    [Tooltip("ringPadding 이후 추가로 랜덤하게 더 벌어질 수 있는 두께")]
    public float ringThickness = 3f;

    [Header("참조 (비워두면 자동으로 찾음)")]
    [Tooltip("비워두면 Camera.main 사용")]
    public Camera targetCamera;
    [Tooltip("비워두면 \"Player\" 태그로 자동 탐색")]
    public Transform player;
    [Tooltip("비워두면 씬에서 자동으로 찾음. 벽/건물 위나 플레이어가 갈 수 없는 고립 구역에 스폰되는 것을 막는 데 사용")]
    public StageMapGenerator mapGenerator;

    [Header("스폰 위치 유효성 검사")]
    [Tooltip("화면 밖 링에서 뽑은 위치가 벽/건물 위이거나 도달 불가 구역이면 다시 뽑는 최대 횟수")]
    [Min(1)] public int maxSpawnPositionAttempts = 10;

    // 지금까지 스폰해서 살아있는(파괴되지 않은) 몬스터 목록
    private readonly List<GameObject> aliveMonsters = new List<GameObject>();

    /// <summary>현재 살아서 스폰돼 있는 몬스터 목록(읽기전용). 매 프레임 CountKills()가 파괴된
    /// 항목을 정리해두므로 미니맵 등 외부에서 새로 스캔할 필요 없이 그대로 순회하면 된다.</summary>
    public IReadOnlyList<GameObject> AliveMonsters => aliveMonsters;
    private float spawnTimer;

    // 이번 스테이지에서 지금까지 스폰한 마릿수(총 한도 체크용, 동시 생존 수와는 별개)
    private int spawnedCount;
    // 이번 스테이지에서 지금까지 잡은 마릿수
    private int killedCount;

    private ZombieCountUI zombieCountUI;
    private ScoreUI scoreUI;

    /// <summary>
    /// 시간 초과 또는 목표 마릿수 달성으로 스테이지가 끝났는지. 켜지면 스폰/피격/이동 등
    /// 게임플레이 로직은 전부 멈추고 스코어 화면(UI)만 동작한다.
    /// </summary>
    public static bool IsGameOver { get; private set; }

    /// <summary>
    /// 목표 마릿수를 전부 잡아서 "진짜로" 클리어했는지. 죽거나 시간 초과로 끝났으면 false다.
    /// TotalScoreUI가 이 값을 보고 별점을 계산/저장할지 결정한다 - 죽었는데 남은 시간이 많다는
    /// 이유로 별점이 매겨지는 것을 막기 위한 값.
    /// </summary>
    public static bool StageCleared { get; private set; }

    /// <summary>
    /// 이번 스테이지의 클리어 결과(별 개수)가 StageProgressManager에 이미 보고됐는지. TotalScoreUI가
    /// 스크립트 재컴파일 등으로 Start()가 두 번 실행되는 것 같은 예외적인 상황에서도 같은 클리어를
    /// 두 번 보고해서 엉뚱한 스테이지에 별이 잘못 기록되는 일이 없도록 한 번 보고되면 true로 잠근다.
    /// </summary>
    public static bool ScoreReported { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 이전 스테이지에서 멈춰 있던 시간/사운드를 새 스테이지 진입 시 항상 되살린다(안전장치).
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsGameOver = false;
        StageCleared = false;
        ScoreReported = false;
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

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<StageMapGenerator>();

        zombieCountUI = FindFirstObjectByType<ZombieCountUI>();
        scoreUI = FindFirstObjectByType<ScoreUI>();

        // 스테이지가 올라갈수록(1스테이지 기준 monstersPerStageIncrease씩) 처치해야 할 목표 마릿수를 늘린다.
        totalMonstersToSpawn += monstersPerStageIncrease * Mathf.Max(0, StageProgressManager.Instance.CurrentStage - 1);

        UpdateZombieCountUI();
    }

    private void Update()
    {
        if (IsGameOver)
            return; // 게임이 끝난 뒤에는 스폰/처치 판정을 더 이상 진행하지 않는다.

        // 죽어서 파괴된(null) 몬스터를 감지해서 처치 수를 늘린다. 스폰 주기와 무관하게 매 프레임 확인한다.
        CountKills();

        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;
        TrySpawn();
    }

    /// <summary>죽어서 파괴된 몬스터를 목록에서 정리하고, 정리된 수만큼 처치 수로 반영한다.</summary>
    private void CountKills()
    {
        int before = aliveMonsters.Count;
        aliveMonsters.RemoveAll(m => m == null);
        int killedJustNow = before - aliveMonsters.Count;

        if (killedJustNow <= 0)
            return;

        killedCount += killedJustNow;
        UpdateZombieCountUI();

        if (scoreUI != null)
            scoreUI.AddScore(scorePerKill * killedJustNow);

        if (killedCount >= totalMonstersToSpawn)
            EndStage(cleared: true);
    }

    private void UpdateZombieCountUI()
    {
        if (zombieCountUI != null)
            zombieCountUI.SetCount(killedCount, totalMonstersToSpawn);
    }

    /// <summary>TimeUI가 제한시간이 다 됐을 때 호출한다. 목표를 못 채웠으므로 클리어가 아니다.</summary>
    public void NotifyTimeUp()
    {
        EndStage(cleared: false);
    }

    /// <summary>
    /// GamePlayerController가 플레이어 사망 시 호출한다. 좀비 전멸/시간초과와 동일하게
    /// 스테이지를 종료시켜야, 죽은 뒤에도 몬스터가 계속 공격/스킬을 쓰며 사운드를 내는 것을 막을 수 있다.
    /// 죽었으므로 클리어가 아니다.
    /// </summary>
    public void NotifyPlayerDied()
    {
        EndStage(cleared: false);
    }

    /// <summary>
    /// 목표 마릿수를 전부 잡았거나 시간이 다 됐거나 죽었을 때 스테이지를 종료한다. 한 번만 실행되며,
    /// Time.timeScale을 0으로 만들어 이동/공격/스폰 등 게임플레이를 멈추고 스코어 화면(UI)만 띄운다.
    /// cleared는 목표 마릿수를 실제로 다 잡았을 때만 true - TotalScoreUI가 별점을 매길지 결정하는 기준이 된다.
    /// </summary>
    private void EndStage(bool cleared)
    {
        if (IsGameOver)
            return;
        IsGameOver = true;
        StageCleared = cleared;
        Time.timeScale = 0f;

        // AudioListener.pause만으로는 이미 재생 중이던 소리가 "일시정지"만 되므로, 좀비 공격/스킬/발자국/BGM 등
        // 남아있던 사운드를 먼저 확실하게 정지시킨 뒤, 스코어보드 전용 사운드만 재생한다.
        StopAllGameplaySounds();
        AudioListener.pause = true;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayScoreboardSfx();

        if (player == null)
            return;

        GamePlayerController playerController = player.GetComponent<GamePlayerController>();
        if (playerController != null)
            playerController.SpawnScoreCanvas();
    }

    /// <summary>
    /// 스코어보드 사운드를 재생하기 직전, 씬에 남아있는 모든 사운드를 즉시 정지시킨다.
    /// ignoreListenerPause가 켜진 소스(스코어보드 전용 사운드)는 지금부터 재생해야 하므로 건드리지 않는다.
    /// </summary>
    private void StopAllGameplaySounds()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in sources)
        {
            if (source.ignoreListenerPause)
                continue;

            source.Stop();
        }
    }

    /// <summary>스폰 조건(카메라/최대 마릿수/총 한도)을 확인하고 spawnPerTick 만큼 스폰을 시도한다.</summary>
    private void TrySpawn()
    {
        if (spawnedCount >= totalMonstersToSpawn)
            return; // 이번 스테이지에서 스폰할 총 마릿수를 다 썼다

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
                return; // 카메라가 없으면 화면 밖 위치를 계산할 수 없음
        }

        for (int i = 0; i < spawnPerTick; i++)
        {
            if (spawnedCount >= totalMonstersToSpawn)
                break;
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

        if (!TryGetSpawnPosition(out Vector3 spawnPos))
            return; // 유효한 위치를 못 찾았으면 이번 시도는 건너뛴다 (다음 스폰 타이밍에 다시 시도)

        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        aliveMonsters.Add(spawned);
        spawnedCount++;
    }

    /// <summary>
    /// mapGenerator가 있으면 맵 전체에서 임의의 칸을 후보로 뽑아, 벽/건물 위이거나 플레이어가 갈 수 없는
    /// 고립 구역인지 검사해서 걸러낸다. maxSpawnPositionAttempts번 안에 유효한 자리를 못 찾으면 실패로 처리한다.
    /// mapGenerator가 없는 경우(맵 없이 이 스크립트를 쓰는 경우 대비)는 기존 화면 밖 링 방식으로 대체한다.
    /// </summary>
    private bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        if (mapGenerator != null && player != null)
            mapGenerator.ComputeReachability(player.position); // 이미 계산됐으면 내부에서 바로 리턴됨

        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            Vector3 candidate = mapGenerator != null ? GetRandomMapPosition() : GetOffScreenPosition();
            if (mapGenerator == null || mapGenerator.IsWorldPositionSpawnable(candidate))
            {
                spawnPos = candidate;
                return true;
            }
        }

        spawnPos = default;
        return false;
    }

    /// <summary>맵 칸 범위(0 ~ Width-1, 0 ~ Height-1) 안에서 임의의 칸 하나를 골라 월드 좌표로 반환한다.</summary>
    private Vector3 GetRandomMapPosition()
    {
        int x = Random.Range(0, mapGenerator.Width);
        int y = Random.Range(0, mapGenerator.Height);
        return mapGenerator.GetCellCenterWorld(x, y);
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
