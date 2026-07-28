using UnityEngine;
using UnityEngine.UI;

// 마우스 포인터 위치에 표시되는 "+" 모양 조준점. OS 기본 커서를 숨기고 대체한다.
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class CrosshairUI : MonoBehaviour
{
    private RectTransform rect;
    private Image image;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        if (image.sprite == null) image.sprite = BuildCrosshairSprite();
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        // PlayUI 1 캔버스는 ScreenSpaceOverlay + CanvasScaler.scaleFactor 1이라
        // Input.mousePosition을 그대로 대입해도 화면 좌표와 정확히 맞는다.
        rect.position = Input.mousePosition;
    }

    // 16x16 텍스처에 "+" 모양을 그려 스프라이트로 감싼다(WeaponVisuals.Placeholder와 동일한 패턴).
    private static Sprite BuildCrosshairSprite()
    {
        const int size = 16;
        const int thickness = 2;
        int mid = size / 2;

        var tex = new Texture2D(size, size);
        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onCross = Mathf.Abs(x - mid) < thickness || Mathf.Abs(y - mid) < thickness;
                tex.SetPixel(x, y, onCross ? white : clear);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
