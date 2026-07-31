using UnityEngine;

/// <summary>
/// StageMapGenerator ���� Ʈ����. 
/// �������� ���� �� ȣ���ϰų�, �����Ϳ��� ��Ŭ�� -> Generate Now�� ��� �׽�Ʈ ����.
/// </summary>
public class MapGenerationTrigger : MonoBehaviour
{
    public StageMapGenerator generator;

    [Tooltip("�׽�Ʈ��: �� ���� �� �� ���������� �������")]
    public int stageIndexForTesting = 1;

    void Start()
    {
        // ���� ���ӿ����� GameManager ���� ������ ���� �������� ��ȣ�� �޾ƿͼ� ȣ��
        int stage = StageProgressManager.HasInstance ? StageProgressManager.Instance.CurrentStage : stageIndexForTesting;
        generator.GenerateStage(stage);
    }

    [ContextMenu("Generate Now")]
    private void GenerateNow()
    {
        generator.GenerateStage(stageIndexForTesting);
    }
}
