using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// 화면 절반짜리 투명 터치 영역 위에서 눌린 지점에 배경 원이 순간이동하는 "플로팅" 조이스틱 공통 로직.
// OnScreenJoystick(이동)과 OnScreenAttackButton(공격)이 이 클래스를 상속해 static 표면만 각자 유지한다
// (GamePlayerController는 각 서브클래스의 static Direction/Held만 읽으므로 여기서 API가 바뀌지 않는다).
public abstract class OnScreenJoystickBase : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // 반경의 이 비율 안쪽은 흔들림 방지용 데드존으로 취급해 0으로 보고한다.
    private const float DeadzoneRatio = 0.1f;

    private RectTransform touchArea;
    private RectTransform background;
    private RectTransform knob;
    private float radius = 1f;

    // 지금 이 조이스틱을 잡고 있는 손가락(터치)의 pointerId. 안 잡혀 있으면 null이라, 두 번째 손가락이
    // 같은 영역에 닿아도 첫 손가락을 놓기 전까지는 무시한다(InputSystemUIInputModule 기준 pointerId는
    // 마우스도 양수라 부호로 터치를 구분할 수 없다 — pointerType으로만 구분한다).
    private int? activePointerId;

    public void Init(RectTransform area, RectTransform bg, RectTransform knobRect, float knobRadius)
    {
        touchArea = area;
        background = bg;
        knob = knobRect;
        radius = Mathf.Max(knobRadius, 0.0001f);

        background.gameObject.SetActive(false);
    }

    // 서브클래스가 눌림/뗌/방향 변화에 반응해 자신의 static 상태를 갱신하는 훅.
    protected virtual void OnPressed() { }
    protected virtual void OnReleased() { }
    protected abstract void OnDirectionChanged(Vector2 analog);

    private static bool IsTouch(PointerEventData eventData) =>
        eventData is ExtendedPointerEventData ext && ext.pointerType == UIPointerType.Touch;

    public void OnPointerDown(PointerEventData eventData)
    {
        // 마우스는 무시한다 — PC는 지금처럼 마우스 조준/클릭 공격 그대로 쓴다.
        if (!IsTouch(eventData)) return;
        // 이미 다른 손가락이 잡고 있으면(예: 같은 영역에 두 번째 손가락) 무시 — 첫 손가락이 계속 우선한다.
        if (activePointerId.HasValue) return;

        activePointerId = eventData.pointerId;

        // 배경 원을 누른 지점으로 옮긴다. touchArea/background 모두 pivot(0.5,0.5)이고 background는
        // anchorMin=anchorMax=(0.5,0.5)라, touchArea 중심 기준 로컬 좌표가 곧 background의
        // anchoredPosition이 된다 (MobileControlsUI가 이 전제를 만족하도록 생성한다).
        if (touchArea != null && background != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(touchArea, eventData.position, eventData.pressEventCamera, out var local))
        {
            background.anchoredPosition = local;
        }

        background.gameObject.SetActive(true);
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        OnDirectionChanged(Vector2.zero);

        OnPressed();
        UpdateKnob(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!activePointerId.HasValue || eventData.pointerId != activePointerId.Value) return;
        UpdateKnob(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!activePointerId.HasValue || eventData.pointerId != activePointerId.Value) return;
        Release();
    }

    private void Release()
    {
        activePointerId = null;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
        OnDirectionChanged(Vector2.zero);
        OnReleased();
    }

    private void UpdateKnob(PointerEventData eventData)
    {
        if (background == null || knob == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var local);

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        knob.anchoredPosition = clamped;

        Vector2 analog = clamped / radius;
        OnDirectionChanged(analog.magnitude < DeadzoneRatio ? Vector2.zero : analog);
    }

    protected virtual void OnDisable()
    {
        // 터치 영역이 꺼지는 동안(일시정지 등) 조이스틱이 눌린 채로 멈추지 않게 정리한다.
        Release();
    }
}
