using UnityEngine;

// 플레이어 머리 위에 항상 표시되는 스프린트 스태미나 게이지. GamePlayerController.GetStamina()/
// GetMaxStamina()를 매 프레임 폴링한다(PlayerHealthBarUI/AmmoBarUI와 동일한 폴링 방식).
// Monster/MonsterHealthBar.cs와 같은 "런타임에 배경/채움 자식 SpriteRenderer 2개를 만드는" 패턴을
// 쓰지만, 몬스터 체력바와 달리 피격 시에만 잠깐 나타나는 게 아니라 항상 보여야 하므로 자동 숨김
// 타이머는 없다. 아트가 없어 WeaponVisuals.Placeholder(1x1 흰 스프라이트, PPU 1)를 배경/채움 색으로
// 각각 틴트해서 쓴다. 플레이어 프리팹 루트는 항상 스케일 1이라(몬스터처럼 프리팹마다 스케일이
// 달라지지 않음) MonsterHealthBar의 부모 스케일 역보정 로직은 필요 없다.
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

    private void Awake()
    {
        playerController = GetComponent<GamePlayerController>();
        BuildBar();
    }

    private void Update()
    {
        float max = playerController.GetMaxStamina();
        float ratio = max > 0f ? Mathf.Clamp01(playerController.GetStamina() / max) : 0f;
        ApplyFillRatio(ratio);
    }

    private void BuildBar()
    {
        var bgGO = new GameObject("StaminaBarBackground");
        bgGO.transform.SetParent(transform, false);
        var bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = WeaponVisuals.Placeholder;
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = sortingOrder;
        bgGO.transform.localScale = new Vector3(width, height, 1f);
        bgGO.transform.localPosition = new Vector3(0f, offsetY, 0f);

        var fillGO = new GameObject("StaminaBarFill");
        fillGO.transform.SetParent(transform, false);
        var fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = WeaponVisuals.Placeholder;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = sortingOrder + 1;
        fillTransform = fillGO.transform;

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
