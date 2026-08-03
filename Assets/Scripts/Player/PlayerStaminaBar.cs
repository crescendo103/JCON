using UnityEngine;

// 플레이어 머리 위에 표시되는 대쉬(스프린트) 스태미나 게이지. GamePlayerController.IsDashUnlocked()가
// false인 동안(필드의 DashPickup을 먹기 전)은 완전히 숨겨져 있다가, 해금되는 순간부터 나타나서
// GetStamina()/GetMaxStamina()를 매 프레임 폴링해 채워진다(PlayerHealthBarUI/AmmoBarUI와 동일한 폴링 방식).
// Monster/MonsterHealthBar.cs와 같은 "런타임에 배경/채움 자식 SpriteRenderer 2개를 만드는" 패턴을 쓴다.
// 아트가 없어 WeaponVisuals.Placeholder(1x1 흰 스프라이트, PPU 1)를 배경/채움 색으로 각각 틴트해서 쓴다.
// 플레이어 프리팹 루트는 항상 스케일 1이라(몬스터처럼 프리팹마다 스케일이 달라지지 않음)
// MonsterHealthBar의 부모 스케일 역보정 로직은 필요 없다.
[RequireComponent(typeof(GamePlayerController))]
public class PlayerStaminaBar : MonoBehaviour
{
    [Header("위치/크기")]
    public float offsetY = 0.95f;
    public float width = 0.5f;
    public float height = 0.08f;

    [Header("색상")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
    public Color fillColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("플레이어 루트에는 SpriteRenderer가 없어 부모 정렬값을 물려받을 수 없으므로, 충분히 큰 값을 직접 지정한다")]
    public int sortingOrder = 10;

    private GamePlayerController playerController;
    private Transform fillTransform;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;

    private void Awake()
    {
        playerController = GetComponent<GamePlayerController>();
        BuildBar();
    }

    private void Update()
    {
        bool unlocked = playerController.IsDashUnlocked();
        bgRenderer.enabled = unlocked;
        fillRenderer.enabled = unlocked;
        if (!unlocked) return;

        float max = playerController.GetMaxStamina();
        float ratio = max > 0f ? Mathf.Clamp01(playerController.GetStamina() / max) : 0f;
        ApplyFillRatio(ratio);
    }

    private void BuildBar()
    {
        var bgGO = new GameObject("StaminaBarBackground");
        bgGO.transform.SetParent(transform, false);
        bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = WeaponVisuals.Placeholder;
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = sortingOrder;
        bgGO.transform.localScale = new Vector3(width, height, 1f);
        bgGO.transform.localPosition = new Vector3(0f, offsetY, 0f);

        var fillGO = new GameObject("StaminaBarFill");
        fillGO.transform.SetParent(transform, false);
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = WeaponVisuals.Placeholder;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = sortingOrder + 1;
        fillTransform = fillGO.transform;

        // 대쉬를 해금하기 전까지는 완전히 숨겨둔다(Update()가 매 프레임 해금 여부로 다시 켜고 끈다).
        bgRenderer.enabled = false;
        fillRenderer.enabled = false;

        ApplyFillRatio(1f);
    }

    // WeaponVisuals.Placeholder는 피벗 (0.5,0.5), PPU 1이라 localScale이 곧 월드 크기가 된다.
    // 왼쪽 끝(barLeft)을 고정한 채 ratio만큼만 오른쪽으로 채워지도록 스케일/위치를 계산한다
    // (MonsterHealthBar.PositionAndScaleLeftAligned와 같은 접근, 스프라이트 bounds 조회는 불필요).
    private void ApplyFillRatio(float ratio)
    {
        float barLeft = -width * 0.5f;
        float filledWidth = width * ratio;

        fillTransform.localScale = new Vector3(filledWidth, height, 1f);
        fillTransform.localPosition = new Vector3(barLeft + filledWidth * 0.5f, offsetY, 0f);
    }
}
