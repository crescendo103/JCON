using System;
using UnityEngine;

/// <summary>
/// 게임 전역 상태/타이머/승패를 관리하는 싱글톤. Game 씬에 1개만 존재.
/// 플레이어 관련 컴포넌트 참조도 이 곳에 모아 다른 시스템(무기, UI)이 쉽게 접근하도록 한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;
    public float ElapsedTime { get; private set; }

    public PlayerController Player { get; private set; }
    public PlayerHealth PlayerHealth { get; private set; }
    public PlayerStats PlayerStats { get; private set; }
    public WeaponInventory Weapons { get; private set; }

    public event Action<GameState> OnStateChanged;

    // 30:00 리퍼 등장, 이후 90초 생존 시 클리어.
    public const float ReaperSpawnTime = 1800f;
    public const float ClearGraceTime = 90f;

    public bool ReaperTriggered { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Application.targetFrameRate = 120; // 목표 프레임레이트 120으로 고정 (VSync가 켜진 품질 레벨에서는 무시됨)
        Time.timeScale = 1f;
        ElapsedTime = 0f;
        State = GameState.Playing;

        // 씬 재시작/전환 시 이전 런의 잔여 정적 상태가 남지 않도록 방어적으로 초기화.
        EnemyTracker.Active.Clear();
    }

    public void RegisterPlayer(PlayerController controller, PlayerHealth health, PlayerStats stats, WeaponInventory weapons)
    {
        Player = controller;
        PlayerHealth = health;
        PlayerStats = stats;
        Weapons = weapons;
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        ElapsedTime += Time.deltaTime;

        if (!ReaperTriggered && ElapsedTime >= ReaperSpawnTime)
        {
            ReaperTriggered = true;
        }

        if (ElapsedTime >= ReaperSpawnTime + ClearGraceTime)
        {
            TriggerClear();
        }
    }

    public void RequestPause()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void RequestResume()
    {
        if (State != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void EnterLevelUp()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.LevelUp);
        Time.timeScale = 0f;
    }

    public void ExitLevelUp()
    {
        if (State != GameState.LevelUp) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void OnPlayerDied()
    {
        if (State == GameState.GameOver || State == GameState.Clear) return;
        SetState(GameState.GameOver);
        Time.timeScale = 0f;
    }

    void TriggerClear()
    {
        if (State == GameState.GameOver || State == GameState.Clear) return;
        SetState(GameState.Clear);
        Time.timeScale = 0f;
    }

    void SetState(GameState s)
    {
        State = s;
        OnStateChanged?.Invoke(s);
    }

    public static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
