using UnityEngine;

/// <summary>현재 런(1회 플레이)의 통계. 결과 화면(GameOver/Clear)에서 표시된다.</summary>
public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    public int KillCount { get; private set; }

    void Awake()
    {
        Instance = this;
        KillCount = 0;
    }

    public void AddKill()
    {
        KillCount++;
    }
}
