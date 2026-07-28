using UnityEngine;

// 무기 스프라이트가 아직 없는 경우(예: 전기톱) 임시 도형으로 대체 표시하기 위한 공용 유틸.
// WeaponPickup(필드 픽업)과 PlayerController(장착) 양쪽에서 동일한 규칙을 쓰기 위해 분리했다.
public static class WeaponVisuals
{
    public const float PlaceholderScale = 0.5f;
    public static readonly Color PlaceholderColor = new Color(1f, 0.55f, 0f, 1f);

    private static Sprite placeholder;

    // 1x1 흰 텍스처를 PPU 1로 감싼 스프라이트. PPU 1이라 localScale 1 = 월드 1유닛 정사각형이 되어
    // PlaceholderScale을 그대로 localScale에 대입하면 된다.
    public static Sprite Placeholder
    {
        get
        {
            if (placeholder == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;

                placeholder = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                placeholder.hideFlags = HideFlags.DontSave;
            }

            return placeholder;
        }
    }

    /// <summary>
    /// source가 있으면 그대로, 없으면 임시 도형(주황색 사각형)으로 대체할 값을 계산한다.
    /// 반환값은 실제 스프라이트가 지정되어 있었는지 여부.
    /// </summary>
    public static bool Resolve(Sprite source, float displayScale, out Sprite sprite, out Color color, out float scale)
    {
        if (source != null)
        {
            sprite = source;
            color = Color.white;
            scale = displayScale;
            return true;
        }

        sprite = Placeholder;
        color = PlaceholderColor;
        scale = PlaceholderScale;
        return false;
    }
}
