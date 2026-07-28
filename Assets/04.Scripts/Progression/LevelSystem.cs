using System;
using UnityEngine;

/// <summary>경험치/레벨 관리. 레벨업 시 LevelUpUI를 호출해 강화 카드를 띄운다.</summary>
public class LevelSystem : MonoBehaviour
{
    public static LevelSystem Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public int CurrentXP { get; private set; }
    public int XPToNext { get; private set; }

    // 06_Content_Tables.md의 레벨업 요구 경험치 곡선 (1->2, 2->3, ...). 20레벨 이후는 +25씩 증가.
    static readonly int[] BaseCurve =
    {
        5, 10, 17, 25, 34, 44, 55, 67, 80, 92,
        105, 119, 134, 150, 167, 185, 204, 224, 245, 267
    };

    /// <summary>level, currentXP, xpToNext</summary>
    public event Action<int, int, int> OnXPChanged;

    void Awake()
    {
        Instance = this;
        Level = 1;
        CurrentXP = 0;
        XPToNext = GetRequirement(1);
    }

    public void AddExperience(int amount)
    {
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;
        if (state == GameState.GameOver || state == GameState.Clear) return;

        CurrentXP += amount;
        OnXPChanged?.Invoke(Level, CurrentXP, XPToNext);
        TryLevelUp();
    }

    public void TryLevelUp()
    {
        if (CurrentXP < XPToNext) return;

        CurrentXP -= XPToNext;
        Level++;
        XPToNext = GetRequirement(Level);
        OnXPChanged?.Invoke(Level, CurrentXP, XPToNext);

        if (LevelUpUI.Instance != null) LevelUpUI.Instance.ShowLevelUp();
    }

    int GetRequirement(int level)
    {
        int idx = level - 1;
        if (idx < BaseCurve.Length) return BaseCurve[idx];
        return BaseCurve[BaseCurve.Length - 1] + (idx - (BaseCurve.Length - 1)) * 25;
    }
}
