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

    [Tooltip("비워두면 \"Player\" 태그로 자동 탐색")]
    public Transform player;

    void Start()
    {
        // ���� ���ӿ����� GameManager ���� ������ ���� �������� ��ȣ�� �޾ƿͼ� ȣ��
        int stage = StageProgressManager.HasInstance ? StageProgressManager.Instance.CurrentStage : stageIndexForTesting;
        generator.GenerateStage(stage);
        PlacePlayerAtRandomStart();
    }

    [ContextMenu("Generate Now")]
    private void GenerateNow()
    {
        generator.GenerateStage(stageIndexForTesting);
        PlacePlayerAtRandomStart();
    }

    // 맵 생성 직후 플레이어를 걸을 수 있는 임의의 칸으로 옮긴다. transform과 Rigidbody2D 위치를 함께
    // 갱신해야 물리 갱신 전까지 위치가 어긋나지 않는다.
    private void PlacePlayerAtRandomStart()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        if (player == null || generator == null) return;

        Vector3 spawnPos = generator.GetRandomWalkableWorldPosition();
        player.position = spawnPos;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = spawnPos;
    }
}
