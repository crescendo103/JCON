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

    [Header("����")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

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

    // ���콺 ���� ���� (Ŭ�� ����)
    public void OnPointerDown(PointerEventData eventData)
    {
        //targetImage.sprite = pressedSprite;
        PlaySound(clickSound);
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
            audioSource.PlayOneShot(clip);
    }
}