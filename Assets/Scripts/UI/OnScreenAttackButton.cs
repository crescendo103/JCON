using UnityEngine;
using UnityEngine.EventSystems;

// 화면 우측 하단의 공격 조이스틱. 이동 조이스틱(OnScreenJoystick)과 완전히 같은 방식으로 배경 원 안에서
// 노브를 드래그해 공격 방향을 직접 조정한다. 동시에 누르고 있는 동안은 마우스 왼쪽 버튼과 동등한
// 공격 입력(Held/눌림)도 겸한다 — GamePlayerController.Click()이 이 값을 함께 읽는다.
public class OnScreenAttackButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // Input.GetKey(Mouse0)에 대응 — 누르고 있는 동안 계속 true (연사/연타 무기용).
    public static bool Held { get; private set; }

    // 안 누르고 있으면 (0,0). OnScreenJoystick.Direction과 같은 규칙: 크기 0~1의 아날로그 값이고,
    // 데드존 안이면 0으로 취급한다. GamePlayerController.GetAimDirection()이 이 값을 조준으로 쓴다.
    public static Vector2 Direction { get; private set; }

    // Input.GetKeyDown(Mouse0)에 대응 — 한 번 읽으면 바로 false로 리셋된다.
    // Update당 정확히 한 번만 ConsumePress()를 호출해야 GetKeyDown과 같은 "이번 프레임에 눌렸는가"
    // 의미가 유지된다(GamePlayerController.Click()이 매 프레임 정확히 한 번 호출).
    private static bool pressedThisFrame;
    private const float DeadzoneRatio = 0.1f;

    public static bool ConsumePress()
    {
        if (!pressedThisFrame) return false;
        pressedThisFrame = false;
        return true;
    }

    private RectTransform background;
    private RectTransform knob;
    private float radius = 1f;

    public void Init(RectTransform bg, RectTransform knobRect, float knobRadius)
    {
        background = bg;
        knob = knobRect;
        radius = Mathf.Max(knobRadius, 0.0001f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Held = true;
        pressedThisFrame = true;
        // 드래그 없이 짧게 탭만 해도 탭한 지점이 바로 방향이 되도록 즉시 반영한다.
        UpdateKnob(eventData);
    }

    public void OnDrag(PointerEventData eventData) => UpdateKnob(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        Held = false;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }

    private void UpdateKnob(PointerEventData eventData)
    {
        if (background == null || knob == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var local);

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        knob.anchoredPosition = clamped;

        Vector2 analog = clamped / radius;
        Direction = analog.magnitude < DeadzoneRatio ? Vector2.zero : analog;
    }

    private void OnDisable()
    {
        Held = false;
        Direction = Vector2.zero;
    }
}
