using System;
using UnityEngine;

/// <summary>
/// 스테이지 클리어 여부와 스테이지별/전체 별 개수를 관리하는 전역 싱글톤.
/// PlayerPrefs에 저장되어 앱을 껐다 켜도 진행도가 유지된다.
/// 씬에 미리 배치할 필요 없이, 어디서든 Instance에 접근하는 순간 자동으로 생성되고
/// DontDestroyOnLoad로 유지되어 다른 UI 씬에서도 그대로 이어서 쓸 수 있다.
/// (참고: Assets/Scripts/Managers/StageManager.cs 는 몬스터 스포너이며 이 스크립트와는 무관하다.)
/// </summary>
public class StageProgressManager : MonoBehaviour
{
    // 게임 구성 - 이 두 값만 바꾸면 전체 별 개수가 자동으로 따라간다.
    public const int StageCount = 18;      // 전체 스테이지 수
    public const int StarsPerStage = 3;    // 스테이지당 최대 별 개수 (ScoreCanvas의 별 3개와 동일)

    private const string KeyPrefix = "JCON_Progress_";
    private const string CurrentStageKey = KeyPrefix + "CurrentStage";

    /// <summary>게임에서 모을 수 있는 별의 총합 (18 * 3 = 54). "6/54" 표시의 분모.</summary>
    public static int MaxTotalStars => StageCount * StarsPerStage;

    private static StageProgressManager instance;
    private static bool isQuitting;

    /// <summary>
    /// 어디서든 이 프로퍼티로 접근한다. 아직 인스턴스가 없으면 스스로 생성하므로 항상 사용 가능하다.
    /// </summary>
    public static StageProgressManager Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                // 씬에 미리 배치해 둔 것이 있으면 그것을 재사용
                instance = FindFirstObjectByType<StageProgressManager>();
                if (instance == null)
                {
                    // 없으면 직접 만든다. AddComponent 시점에 Awake가 즉시 실행되어 instance가 채워진다.
                    GameObject holder = new GameObject("StageProgressManager");
                    instance = holder.AddComponent<StageProgressManager>();
                }
            }
            return instance;
        }
    }

    /// <summary>매니저가 이미 생성되어 있는지 확인만 한다 (없으면 새로 만들지 않는다).</summary>
    public static bool HasInstance => instance != null;

    /// <summary>
    /// 첫 씬이 로드되기 전에 매니저를 미리 만들어 둔다.
    /// 덕분에 씬이나 프리팹을 건드리지 않고도 어떤 씬을 열든 항상 존재한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        isQuitting = false;
        _ = Instance; // 접근만으로 생성 + DontDestroyOnLoad 처리가 끝난다
    }

    /// <summary>클리어 여부/별 개수가 바뀔 때마다 호출된다. UI가 구독해서 새로고침한다.</summary>
    public event Action OnProgressChanged;

    // 내부 배열은 0-based(인덱스 0 = 스테이지 1), 외부 API는 전부 1-based로 다룬다.
    private int[] stageStars;
    private int currentStage = 1;

    /// <summary>
    /// 지금 플레이할(플레이 중인) 스테이지 번호 (1 ~ StageCount).
    /// 스테이지 선택 UI에서 스테이지 씬을 로드하기 "전에" 지정한다. 씬 이름으로 추측하지 않는다.
    /// </summary>
    public int CurrentStage
    {
        get => currentStage;
        set
        {
            int clamped = Mathf.Clamp(value, 1, StageCount);
            if (clamped == currentStage)
                return;

            currentStage = clamped;
            PlayerPrefs.SetInt(CurrentStageKey, currentStage);
            PlayerPrefs.Save();
        }
    }

    /// <summary>지금까지 모은 별의 총합. "6/54" 표시의 분자.</summary>
    public int TotalStars { get; private set; }

    /// <summary>별을 1개 이상 얻어 클리어한 스테이지 개수.</summary>
    public int ClearedStageCount { get; private set; }

    private void Awake()
    {
        // 하우스 컨벤션: 중복 인스턴스는 파괴 (StageManager.cs와 동일한 가드)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>스테이지 번호가 유효 범위(1 ~ StageCount)인지.</summary>
    public static bool IsValidStage(int stage) => stage >= 1 && stage <= StageCount;

    /// <summary>해당 스테이지에서 얻은 최고 별 개수 (0 ~ StarsPerStage).</summary>
    public int GetStars(int stage) => IsValidStage(stage) ? stageStars[stage - 1] : 0;

    /// <summary>별을 1개 이상 얻었으면 클리어로 간주한다.</summary>
    public bool IsCleared(int stage) => GetStars(stage) > 0;

    /// <summary>
    /// 이 스테이지를 지금 플레이할 수 있는지. 1스테이지는 항상 열려 있고,
    /// 그 외에는 바로 이전 스테이지를 별 3개(만점)로 깨야 열린다.
    /// 별 1~2개로는 다음 스테이지가 열리지 않는다.
    /// 스테이지 선택 UI에서 Unlocked/Locked 표시를 나누는 기준으로 쓴다.
    /// </summary>
    public bool IsUnlocked(int stage)
    {
        if (!IsValidStage(stage))
            return false;
        if (stage == 1)
            return true;

        return GetStars(stage - 1) >= StarsPerStage;
    }

    /// <summary>
    /// 스테이지 결과를 기록한다. 별은 "최고 기록"만 남는다 -
    /// 재도전해서 더 적게 받아도 이미 모은 별은 줄어들지 않는다.
    /// </summary>
    /// <returns>이전 기록보다 좋아져서 실제로 갱신되었으면 true.</returns>
    public bool SetStageResult(int stage, int stars)
    {
        if (!IsValidStage(stage))
        {
            Debug.LogWarning($"[StageProgressManager] 잘못된 스테이지 번호: {stage} (1~{StageCount})");
            return false;
        }

        int clampedStars = Mathf.Clamp(stars, 0, StarsPerStage);
        int index = stage - 1;

        if (clampedStars <= stageStars[index])
            return false; // 이전 기록보다 낮거나 같으면 갱신하지 않음 (최고 기록 유지)

        stageStars[index] = clampedStars;
        PlayerPrefs.SetInt(KeyPrefix + stage, clampedStars);
        PlayerPrefs.Save();

        RecalculateTotals();
        OnProgressChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// CurrentStage에 대한 결과를 기록하는 편의 메서드. ScoreCanvas(TotalScoreUI) 등에서 호출한다.
    /// 별 3개(만점)로 깼을 때만 CurrentStage를 다음 스테이지로 넘긴다.
    /// 그렇지 않으면 CurrentStage를 그대로 유지해서, 다시 플레이하면 같은 스테이지로 들어가게 한다.
    /// </summary>
    public bool ReportCurrentStageResult(int stars)
    {
        bool improved = SetStageResult(currentStage, stars);

        if (Mathf.Clamp(stars, 0, StarsPerStage) >= StarsPerStage && currentStage < StageCount)
            CurrentStage = currentStage + 1;

        return improved;
    }

    /// <summary>저장된 진행도를 전부 초기화한다 (디버그/테스트용). 인스펙터 우클릭으로도 실행 가능.</summary>
    [ContextMenu("진행도 초기화")]
    public void ResetProgress()
    {
        for (int stage = 1; stage <= StageCount; stage++)
            PlayerPrefs.DeleteKey(KeyPrefix + stage);
        PlayerPrefs.DeleteKey(CurrentStageKey);
        PlayerPrefs.Save();

        Load();
        OnProgressChanged?.Invoke();
    }

    private void Load()
    {
        stageStars = new int[StageCount];
        for (int i = 0; i < StageCount; i++)
        {
            // 설정을 나중에 줄여도(예: StarsPerStage 3 -> 1) 이상한 값이 들어오지 않도록 Clamp
            stageStars[i] = Mathf.Clamp(PlayerPrefs.GetInt(KeyPrefix + (i + 1), 0), 0, StarsPerStage);
        }

        currentStage = Mathf.Clamp(PlayerPrefs.GetInt(CurrentStageKey, 1), 1, StageCount);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        int stars = 0;
        int cleared = 0;
        for (int i = 0; i < StageCount; i++)
        {
            stars += stageStars[i];
            if (stageStars[i] > 0)
                cleared++;
        }

        TotalStars = stars;
        ClearedStageCount = cleared;
    }
}
