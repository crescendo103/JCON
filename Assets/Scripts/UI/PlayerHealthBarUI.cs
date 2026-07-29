
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("플레이어 피 이미지 10개")]
    public Image[] healthImages;

    [Header("플레이어 컨트롤러")]
    public GamePlayerController playerController;

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