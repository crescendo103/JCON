using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonStateEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("�̹���")]
    private Image targetImage;
    private Sprite normalSprite;
    public Sprite hoverSprite;
    //public Sprite pressedSprite;

    [Header("���� (ȣ�� ��������Ʈ�� ���� �� ��� ���)")]
    [Tooltip("hoverSprite�� ��� ������ ��������Ʈ ��� �� ���İ����� ȣ�� ȿ���� ����")]
    public float hoverAlpha = 0.6f;
    private float normalAlpha;

    [Header("사운드")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("토글 스프라이트 (PauseButton 등, 클릭마다 두 스프라이트를 번갈아 표시)")]
    [Tooltip("체크하면 클릭할 때마다 toggleSpriteOn/Off를 번갈아 표시한다. 처음엔 원래 스프라이트가 그대로 유지된다")]
    public bool useToggleSprite = false;
    [Tooltip("토글 On 상태(첫 클릭 후)에 표시할 스프라이트")]
    public Sprite toggleSpriteOn;
    [Tooltip("토글 Off 상태(다시 클릭한 후)에 표시할 스프라이트")]
    public Sprite toggleSpriteOff;

    private bool isToggledOn = false;
    private bool isHovering = false;

    void Awake()
    {
        targetImage = GetComponent<Image>();

        // 인스펙터에 드래그해두지 않은 버튼이 많아 audioSource가 null인 채로 남아있던 게
        // PlayOneShot에서 NullReferenceException이 나서 클릭/호버 사운드가 전혀 재생되지 않던 원인이었다.
        // StageButtonUI와 동일하게 코드로 자동 연결한다 (팀 규칙).
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        RefreshNormalState();
    }

    // ���콺 �÷��� ��
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        ApplyHoverState(true);
        PlaySound(hoverSound);
    }

    // ���콺 ����� ��
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        ApplyHoverState(false);
    }

    // 마우스 버튼 누름 (클릭 시작)
    public void OnPointerDown(PointerEventData eventData)
    {
        //targetImage.sprite = pressedSprite;
        PlaySound(clickSound);
        ApplyToggleSprite();
    }

    // 클릭마다 On/Off 스프라이트를 번갈아 표시한다. normalSprite도 같이 갱신해서, 이후 호버가
    // 끝나(ApplyHoverState) 기본 상태로 되돌아갈 때 방금 토글된 스프라이트가 유지되게 한다.
    void ApplyToggleSprite()
    {
        if (!useToggleSprite) return;

        isToggledOn = !isToggledOn;
        Sprite next = isToggledOn ? toggleSpriteOn : toggleSpriteOff;
        if (next == null) return;

        targetImage.sprite = next;
        normalSprite = next;
    }

    // ���콺 ���� ��
    public void OnPointerUp(PointerEventData eventData)
    {
        // �� �� ���� ��ư ���� ������ ȣ�� ���·�, ������� �⺻ ���·�
        ApplyHoverState(isHovering);
    }

    // hoverSprite�� ������ ��������Ʈ�� �ٲٰ�, ������ ���İ��� �ٲ㼭 ȣ�� ȿ���� ǥ���Ѵ�
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

    // �ٸ� ��ũ��Ʈ(StageButtonUI ��)�� targetImage�� ��������Ʈ/���ĸ� ��Ÿ�ӿ� �ٲ� �� ȣ���Ѵ�.
    // �׷��� ������ ȣ���� ������ �� Start ������ ĳ���� �� ���� ������ �ǵ��ư�������.
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
            audioSource.PlayOneShot(clip, SoundSettings.SfxVolume);
    }
}