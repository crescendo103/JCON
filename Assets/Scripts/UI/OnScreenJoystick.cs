using UnityEngine;
using UnityEngine.EventSystems;

// 화면 좌측 하단의 이동용 아날로그 조이스틱. 배경 원 안에서 노브가 드래그를 따라오고,
// 놓으면 중앙으로 돌아온다. GamePlayerController는 이 static Direction만 읽으면 되므로
// 씬에 이 컴포넌트가 있는지 없는지 신경 쓸 필요가 없다(기본값 Vector2.zero라 안전).
public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // 노브를 안 잡고 있으면 (0,0). 크기는 0~1의 아날로그 값(끝까지 밀어야 1) — GamePlayerController가
    // 이동 속도에 그대로 곱하므로 살짝만 밀면 천천히 움직인다.
    public static Vector2 Direction { get; private set; }

    private RectTransform background;
    private RectTransform knob;
    private float radius = 1f;
    // 반경의 이 비율 안쪽은 흔들림 방지용 데드존으로 취급해 0으로 보고한다.
    private const float DeadzoneRatio = 0.1f;

    public void Init(RectTransform bg, RectTransform knobRect, float knobRadius)
    {
        background = bg;
        knob = knobRect;
        radius = Mathf.Max(knobRadius, 0.0001f);
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateKnob(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateKnob(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
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
        // 오브젝트가 꺼지는 동안 조이스틱이 눌린 채로 멈추지 않게 정리한다.
        Direction = Vector2.zero;
    }
}
