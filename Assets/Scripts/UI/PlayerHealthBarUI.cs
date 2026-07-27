
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("체력바를 구성하는 이미지들 (10개)")]
    public Image[] healthImages;

    [Header("참조할 플레이어 체력 스크립트(디버그용) 알아서 찾음")]
    public PlayerController playerController; // 본인 프로젝트의 체력 스크립트로 교체

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }
    void Update()
    {
        UpdateHealthBar(playerController.GetHealth(), playerController.GetMaxHealth());
    }

    void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float percent = currentHealth / maxHealth; // 0.0 ~ 1.0

        // 10%마다 하나씩 -> 몇 개를 켜야 하는지 계산
        int activeCount = Mathf.CeilToInt(percent * healthImages.Length);

        for (int i = 0; i < healthImages.Length; i++)
        {
            healthImages[i].enabled = (i < activeCount);
        }
    }
}