
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("ü�¹ٸ� �����ϴ� �̹����� (10��)")]
    public Image[] healthImages;

    [Header("������ �÷��̾� ü�� ��ũ��Ʈ(����׿�) �˾Ƽ� ã��")]
    public GamePlayerController playerController; // ���� ������Ʈ�� ü�� ��ũ��Ʈ�� ��ü

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<GamePlayerController>();
        }
    }
    void Update()
    {
        UpdateHealthBar(playerController.GetHealth(), playerController.GetMaxHealth());
    }

    void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float percent = currentHealth / maxHealth; // 0.0 ~ 1.0

        // 10%���� �ϳ��� -> �� ���� �Ѿ� �ϴ��� ���
        int activeCount = Mathf.CeilToInt(percent * healthImages.Length);

        for (int i = 0; i < healthImages.Length; i++)
        {
            healthImages[i].enabled = (i < activeCount);
        }
    }
}