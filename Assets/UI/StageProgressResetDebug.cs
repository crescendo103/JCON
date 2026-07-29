using UnityEngine;

/// <summary>
/// 테스트용: 키 하나로 스테이지 진행도(별 개수, CurrentStage)를 전부 초기화한다.
/// 씬에 미리 배치할 필요 없이 게임 시작 시 자동으로 생성된다.
/// 에디터와 개발 빌드에서만 동작하고, 실제 배포(릴리즈) 빌드에는 포함되지 않는다.
/// </summary>
public class StageProgressResetDebug : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("초기화 키")]
    public KeyCode resetKey = KeyCode.F9;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject holder = new GameObject("StageProgressResetDebug");
        holder.AddComponent<StageProgressResetDebug>();
        DontDestroyOnLoad(holder);
    }

    private void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            StageProgressManager.Instance.ResetProgress();
            Debug.Log("[StageProgressResetDebug] " + resetKey + " 눌러서 스테이지 진행도를 초기화했다.");
        }
    }
#endif
}
