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
        normalSprite = targetImage.sprite;
    }

    // 마우스 올렸을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetImage.sprite = hoverSprite;
        PlaySound(hoverSound);
    }

    // 마우스 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetImage.sprite = normalSprite;
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
        // 뗄 때 아직 버튼 위에 있으면 호버 이미지로, 벗어났으면 기본 이미지로
        targetImage.sprite = isHovering ? hoverSprite : normalSprite;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}