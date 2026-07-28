using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플로팅 가상 조이스틱. 화면 터치 영역(TouchArea) 아무 곳이나 누르면 그 위치에 배경이 나타나고,
/// 드래그한 방향/거리를 정규화한 입력 벡터로 제공한다. 배경/핸들 오브젝트는 MCP로 미리 배치된 것을 사용한다.
/// </summary>
public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;
    [SerializeField] float radius = 150f;
    [SerializeField, Range(0f, 0.9f)] float deadZone = 0.2f;

    public Vector2 InputVector { get; private set; }

    void Awake()
    {
        if (background != null) background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        var parent = background.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            background.anchoredPosition = localPoint;
        }

        background.gameObject.SetActive(true);
        handle.anchoredPosition = Vector2.zero;
        InputVector = Vector2.zero;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
            handle.anchoredPosition = clamped;

            float normalizedMagnitude = clamped.magnitude / radius;
            if (normalizedMagnitude < deadZone)
            {
                // 중심 근처의 미세한 떨림이 그대로 이동 입력으로 들어가 캐릭터가 제자리에서
                // 떨리는 것을 방지하기 위한 데드존.
                InputVector = Vector2.zero;
            }
            else
            {
                float remapped = (normalizedMagnitude - deadZone) / (1f - deadZone);
                InputVector = clamped.normalized * remapped;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }
}
