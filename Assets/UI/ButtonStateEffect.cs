using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonStateEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("이미지")]
    private Image targetImage;
    private Sprite normalSprite;
    public Sprite hoverSprite;
    //public Sprite pressedSprite;

    [Header("알파 (호버 스프라이트가 없을 때 대신 사용)")]
    [Tooltip("hoverSprite가 비어 있으면 스프라이트 대신 이 알파값으로 호버 효과를 낸다")]
    public float hoverAlpha = 0.6f;
    private float normalAlpha;

    [Header("사운드")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private bool isHovering = false;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    void Start()
    {
        RefreshNormalState();
    }

    // 마우스 올렸을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        ApplyHoverState(true);
        PlaySound(hoverSound);
    }

    // 마우스 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        ApplyHoverState(false);
    }

    // 마우스 누른 순간 (클릭 시작)
    public void OnPointerDown(PointerEventData eventData)
    {
        //targetImage.sprite = pressedSprite;
        PlaySound(clickSound);
    }

    // 마우스 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        // 뗄 때 아직 버튼 위에 있으면 호버 상태로, 벗어났으면 기본 상태로
        ApplyHoverState(isHovering);
    }

    // hoverSprite가 있으면 스프라이트를 바꾸고, 없으면 알파값만 바꿔서 호버 효과를 표현한다
    void ApplyHoverState(bool hovering)
    {
        if (hoverSprite != null)
        {
            targetImage.sprite = hovering ? hoverSprite : normalSprite;
            return;
        }

        SetAlpha(hovering ? hoverAlpha : normalAlpha);
    }

    void SetAlpha(float alpha)
    {
        Color color = targetImage.color;
        color.a = alpha;
        targetImage.color = color;
    }

    // 다른 스크립트(StageButtonUI 등)가 targetImage의 스프라이트/알파를 런타임에 바꾼 뒤 호출한다.
    // 그렇지 않으면 호버가 끝났을 때 Start 시점에 캐시해 둔 예전 값으로 되돌아가버린다.
    public void RefreshNormalState()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        normalSprite = targetImage.sprite;
        normalAlpha = targetImage.color.a;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}