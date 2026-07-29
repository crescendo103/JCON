using UnityEngine;
using UnityEngine.UI;

// 남은 탄약을 아이콘 10개의 점등 개수로 표시한다. PlayerHealthBarUI와 동일하게
// GamePlayerController를 매 프레임 폴링한다(이 프로젝트에는 UI 이벤트 규약이 없음).
public class AmmoBarUI : MonoBehaviour
{
    [Header("탄약 이미지 10개")]
    public Image[] ammoImages;

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

    private void Update()
    {
        if (playerController == null || ammoImages == null) return;

        int current, max;
        // 맨손·전기톱처럼 탄약 개념이 없는 무기는 '무제한'이므로 바를 꽉 찬 상태로 둔다.
        if (!playerController.TryGetAmmo(out current, out max))
        {
            SetActiveCount(ammoImages.Length);
            return;
        }

        // 무기마다 최대 탄약이 달라(샷건 10·라이플 30·스나 5) 절대 개수로는 10칸을 못 채우거나
        // 넘치므로, 남은 비율로 환산해 점등한다. PlayerHealthBarUI와 같은 CeilToInt 규칙:
        // 1발이라도 남아 있으면 최소 1칸이 켜지고, 0발이면 전부 꺼진다.
        float percent = max > 0 ? (float)current / max : 0f;
        SetActiveCount(Mathf.CeilToInt(percent * ammoImages.Length));
    }

    private void SetActiveCount(int activeCount)
    {
        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] != null) ammoImages[i].enabled = (i < activeCount);
        }
    }
}
