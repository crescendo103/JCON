/// <summary>
/// 게임의 최상위 상태. GameManager가 관리하며, 각 UI/시스템은 이 상태를 참조해 동작 여부를 결정한다.
/// </summary>
public enum GameState
{
    Playing,
    Paused,
    LevelUp,
    GameOver,
    Clear
}
