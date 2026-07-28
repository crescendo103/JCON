using UnityEngine.SceneManagement;

/// <summary>씬 전환 정적 헬퍼. MainMenu/Game 씬 이름은 Build Settings에 등록된 이름과 일치해야 한다.</summary>
public static class GameFlowManager
{
    public const string MainMenuScene = "MainMenu";
    public const string GameScene = "Game";

    public static void LoadGame()
    {
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    public static void RestartGame()
    {
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    public static void LoadMainMenu()
    {
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void QuitGame()
    {
        UnityEngine.Application.Quit();
    }
}
