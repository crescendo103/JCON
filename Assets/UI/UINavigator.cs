using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 버튼 클릭 등으로 UI 화면을 전환할 때 쓰는 매니저.
/// 지정한 씬을 열고(이미 그 씬이면 새로 로드하지 않는다), 그 씬의 최상위 오브젝트 중
/// "UICanvas" 태그가 붙은 것들을 훑어서 이름이 일치하는 것만 켜고 나머지는 다 끈다.
/// 씬에 미리 배치할 필요 없이 Instance에 접근하는 순간 자동 생성되고, 씬이 바뀌어도 유지된다.
/// (ScoreCanvas, StartSceneCanvas, SelectStageUI 오브젝트에는 이미 UICanvas 태그가 붙어 있다)
/// </summary>
public class UINavigator : MonoBehaviour
{
    /// <summary>최상위 UI 캔버스 오브젝트에 붙어 있어야 하는 태그 (Project Settings > Tags and Layers).</summary>
    public const string CanvasTag = "UICanvas";

    /// <summary>UI 화면들이 모여 있는 씬 이름.</summary>
    public const string UISceneName = "UIScene";

    /// <summary>스테이지(게임플레이) 씬 이름. 지금은 스테이지가 전부 이 씬 하나를 공유해서 쓴다.</summary>
    public const string StageSceneName = "kimdongjuplayer 1";

    private static UINavigator instance;

    /// <summary>어디서든 이 프로퍼티로 접근한다. 아직 없으면 스스로 생성하므로 항상 사용 가능하다.</summary>
    public static UINavigator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UINavigator>();
                if (instance == null)
                {
                    GameObject holder = new GameObject("UINavigator");
                    instance = holder.AddComponent<UINavigator>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 하우스 컨벤션: 중복 인스턴스는 파괴 (StageProgressManager와 동일한 가드)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// sceneName 씬을 열고(이미 그 씬이면 로드를 생략한다), 그 씬의 최상위 오브젝트 중
    /// CanvasTag가 붙은 것들 가운데 canvasName과 이름이 같은 것만 켜고 나머지는 끈다.
    /// GameObject.Find는 비활성 오브젝트를 찾지 못하기 때문에, 씬의 루트 오브젝트를 직접 훑어서 찾는다.
    /// </summary>
    public void OpenCanvas(string sceneName, string canvasName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
            SceneManager.LoadScene(sceneName);

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        bool found = false;

        foreach (GameObject root in roots)
        {
            if (!root.CompareTag(CanvasTag))
                continue;

            bool isTarget = root.name == canvasName;
            root.SetActive(isTarget);

            if (isTarget)
                found = true;
        }

        if (!found)
            Debug.LogWarning("[UINavigator] '" + canvasName + "' 이름의 " + CanvasTag + " 태그 오브젝트를 " + sceneName + " 씬에서 찾지 못했다.");
    }

    // 예시: 홈 버튼(또는 홈 키)을 누르면 이 메서드를 호출하면 된다.
    public void OpenStartSceneCanvas()
    {
        OpenCanvas(UISceneName, "StartSceneCanvas");
    }

    public void OpenStageSelectCanvas()
    {
        OpenCanvas(UISceneName, "SelectStageUI");
    }

    public void OpenScoreCanvas()
    {
        OpenCanvas(UISceneName, "ScoreCanvas");
    }

    /// <summary>
    /// 스테이지 선택 화면에서 스테이지 버튼을 눌렀을 때 호출한다.
    /// CurrentStage를 지정한 뒤 스테이지 씬을 로드한다 (지금은 스테이지 전부 씬 하나를 공유).
    /// </summary>
    public void OpenStageScene(int stageNumber)
    {
        StageProgressManager.Instance.CurrentStage = stageNumber;
        SceneManager.LoadScene(StageSceneName);
    }
}
