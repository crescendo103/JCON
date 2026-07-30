using UnityEngine;

/// <summary>
/// StageMapGenerator 실행 트리거. 
/// 스테이지 시작 시 호출하거나, 에디터에서 우클릭 -> Generate Now로 즉시 테스트 가능.
/// </summary>
public class MapGenerationTrigger : MonoBehaviour
{
    public StageMapGenerator generator;

    [Tooltip("테스트용: 이 씬을 몇 번 스테이지로 취급할지")]
    public int stageIndexForTesting = 1;

    void Start()
    {
        // 실제 게임에서는 GameManager 같은 곳에서 현재 스테이지 번호를 받아와서 호출
        generator.GenerateStage(stageIndexForTesting);
    }

    [ContextMenu("Generate Now")]
    private void GenerateNow()
    {
        generator.GenerateStage(stageIndexForTesting);
    }
}
