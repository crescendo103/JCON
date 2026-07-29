using UnityEngine;

// 무기 스프라이트가 아직 없는 경우(예: 전기톱) 임시 도형으로 대체 표시하기 위한 공용 유틸.
// WeaponPickup(필드 픽업)과 GamePlayerController(장착) 양쪽에서 동일한 규칙을 쓰기 위해 분리했다.
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

    private static Sprite chainsawIcon;

    // 실물 아트가 없는 전기톱에 쓰는 절차형 아이콘. Placeholder와 동일한 지연 캐시 패턴.
    public static Sprite ChainsawIcon
    {
        get
        {
            if (chainsawIcon == null) chainsawIcon = BuildChainsawIcon();
            return chainsawIcon;
        }
    }

    // 48x28 텍스처에 전기톱(손잡이/모터하우징/가이드바+체인 톱니)을 직접 그려 스프라이트로 감싼다.
    // WeaponPickup.BuildParachuteSprite()와 동일한 절차형 스프라이트 기법(픽셀 단위 SetPixel).
    private static Sprite BuildChainsawIcon()
    {
        const int w = 48;
        const int h = 28;

        var clear = new Color(0f, 0f, 0f, 0f);
        var grip = new Color(0.75f, 0.25f, 0.1f, 1f);
        var motor = new Color(0.35f, 0.35f, 0.38f, 1f);
        var bar = new Color(0.75f, 0.77f, 0.8f, 1f);
        var tooth = new Color(0.15f, 0.15f, 0.17f, 1f);
        var outline = Color.black;

        var gripRect = new RectInt(2, 16, 8, 9);
        var motorRect = new RectInt(2, 4, 18, 13);
        var barRect = new RectInt(16, 9, 30, 5);

        var colors = new Color[w * h];
        var filled = new bool[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                var p = new Vector2Int(x, y);
                Color c = clear;
                bool isFilled = false;

                if (gripRect.Contains(p)) { c = grip; isFilled = true; }
                else if (motorRect.Contains(p)) { c = motor; isFilled = true; }
                else if (barRect.Contains(p))
                {
                    // 가이드바 윗면을 3픽셀 간격으로 파서 체인 톱니처럼 보이게 한다.
                    bool isTooth = y == barRect.yMax - 1 && (x - barRect.x) % 3 == 0;
                    c = isTooth ? tooth : bar;
                    isFilled = true;
                }

                colors[i] = c;
                filled[i] = isFilled;
            }
        }

        // 채워진 픽셀 중 빈 픽셀과 맞닿은 가장자리를 검정 윤곽선으로 덮어 실루엣을 또렷하게 한다.
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!filled[i]) continue;

                bool onEdge = x == 0 || x == w - 1 || y == 0 || y == h - 1
                    || !filled[i - 1] || !filled[i + 1] || !filled[i - w] || !filled[i + w];

                if (onEdge) colors[i] = outline;
            }
        }

        var tex = new Texture2D(w, h);
        tex.SetPixels(colors);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
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
