using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 스테이지 선택 화면의 버튼 하나(Stage1button 등)에 붙인다.
/// StageProgressManager의 진행도에 따라 하위 "Image" 오브젝트의 스프라이트를 바꾼다.
///  - 클리어했거나 지금 클리어해야 하는(다음) 스테이지 -> clearedSprite (Level/Button/Dummy.png)
///  - 아직 도달하지 못한 스테이지                -> lockedSprite  (Level/Button/Locked.png)
/// 클리어했거나 지금 클리어할 차례(= unlocked)라면 하위 "StarImage" 칸에 별 개수(0~3)에 맞는
/// 이미지를 표시한다. 아직 클리어 전이면 별 0개(0-3.png)가 그대로 보인다 - 숨기지 않는다.
/// ButtonStateEffect와 동일한 방식(AudioSource + AudioClip)으로 호버/클릭 효과음도 낼 수 있다.
/// </summary>
[RequireComponent(typeof(Button))]
public class StageButtonUI : MonoBehaviour, IPointerEnterHandler
{
    [Header("스테이지 번호")]
    [Tooltip("이 버튼이 나타내는 스테이지 번호 (1 ~ StageProgressManager.StageCount)")]
    public int stageNumber = 1;

    [Header("스테이지 상태 이미지 (Level/Button)")]
    [Tooltip("클리어했거나 지금 클리어해야 하는 스테이지일 때 표시 (Dummy.png)")]
    public Sprite clearedSprite;
    [Tooltip("아직 도달하지 못한 스테이지일 때 표시 (Locked.png)")]
    public Sprite lockedSprite;

    [Header("별 개수 이미지 (Level/Star/Group, 클리어했거나 지금 클리어할 차례일 때 표시)")]
    [Tooltip("인덱스 = 별 개수. 0-3.png, 1-3.png, 2-3.png, 3-3.png 순서로 넣는다")]
    public Sprite[] starSprites = new Sprite[4];

    [Header("호버 연출")]
    [Tooltip("클릭 가능한(클리어했거나 지금 클리어할 차례인) 스테이지 버튼에 마우스를 올렸을 때, 스프라이트 대신 이 알파값으로 살짝 어둡게 표시한다")]
    public float unlockedHoverAlpha = 0.8f;

    [Header("효과음")]
    [Tooltip("클릭 가능한 스테이지 버튼에 마우스를 올렸을 때 재생할 사운드")]
    public AudioClip hoverSound;
    [Tooltip("스테이지 버튼을 클릭했을 때 재생할 사운드")]
    public AudioClip clickSound;

    private AudioSource audioSource;
    private Button button;
    private Image stateImage;
    private GameObject starSlot;
    private Image starImage;
    private ButtonStateEffect hoverEffect;
    private GameObject nameText;

    private void Awake()
    {
        // 인스펙터 드래그 연결 대신, 이름으로 찾아 코드로 연결한다 (팀 규칙)
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClickButton);

        // ButtonStateEffect와 동일하게, 효과음용 AudioSource가 없으면 코드로 추가해서 항상 쓸 수 있게 한다.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        Transform imageTransform = transform.Find("Image");
        stateImage = imageTransform?.GetComponent<Image>();
        hoverEffect = imageTransform?.GetComponent<ButtonStateEffect>();

        Transform starSlotTransform = transform.Find("StarImage");
        if (starSlotTransform != null)
        {
            starSlot = starSlotTransform.gameObject;
            starImage = starSlotTransform.GetComponent<Image>();
        }

        Transform textTransform = transform.Find("Text (TMP)");
        if (textTransform != null)
            nameText = textTransform.gameObject;
    }

    private void OnEnable()
    {
        StageProgressManager.Instance.OnProgressChanged += Refresh;
        Refresh();
    }

    // OnEnable의 Refresh() 호출은 다른 씬(예: UIScene)이 통째로 새로 로드되는 첫 프레임에는
    // 초기화 순서가 흔들려서 스프라이트가 반영 안 된 채로 남는 경우가 있었다(스테이지 버튼 여러 개가
    // 하얗게 나오는 버그). Start()는 씬의 모든 오브젝트가 Awake/OnEnable을 마친 뒤에 실행되므로,
    // 여기서 한 번 더 Refresh()를 호출해서 안전망 역할을 하게 한다.
    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        // Instance를 그대로 쓰면 종료 중에 매니저를 다시 만들어버릴 수 있으므로 HasInstance로만 확인한다.
        if (StageProgressManager.HasInstance)
            StageProgressManager.Instance.OnProgressChanged -= Refresh;
    }

    /// <summary>진행도에 맞춰 상태 이미지와 별 개수 이미지를 새로고침한다.</summary>
    public void Refresh()
    {
        StageProgressManager progress = StageProgressManager.Instance;
        bool cleared = progress.IsCleared(stageNumber);
        bool unlocked = progress.IsUnlocked(stageNumber);
        bool isCurrent = unlocked && !cleared; // 지금 클리어해야 하는(다음) 스테이지

        if (stateImage != null)
            stateImage.sprite = unlocked ? clearedSprite : lockedSprite;

        // 아직 도달 못한 스테이지는 이름 텍스트를 숨기고, 클리어했거나 지금 클리어할 차례일 때만 보여준다.
        if (nameText != null)
            nameText.SetActive(cleared || isCurrent);

        // 클리어한 스테이지는 다시 들어갈 수 있어야 하므로 unlocked 기준으로 클릭 가능 여부를 정한다.
        // (한번 unlocked된 스테이지는 나중에 클리어해도 계속 unlocked 상태라 재도전이 막히지 않는다)
        if (button != null)
            button.interactable = unlocked; // 아직 도달 못한 스테이지는 false -> 버튼 비활성화

        if (starSlot != null)
        {
            starSlot.SetActive(unlocked);

            if (unlocked && starImage != null)
            {
                int stars = Mathf.Clamp(progress.GetStars(stageNumber), 0, starSprites.Length - 1);
                starImage.sprite = starSprites[stars];
            }
        }

        // 클릭 가능한(클리어했거나 지금 클리어할 차례인) 스테이지는 호버 시 스프라이트 대신 알파로 연출한다.
        if (hoverEffect != null)
        {
            // 아직 도달 못한(잠긴) 스테이지는 호버 효과 자체를 끈다.
            // ButtonStateEffect는 Button.interactable과 무관하게 자체적으로 포인터 이벤트를 받기 때문에,
            // 컴포넌트를 직접 꺼야 OnPointerEnter/Exit가 아예 호출되지 않는다.
            hoverEffect.enabled = unlocked;

            if (unlocked)
            {
                hoverEffect.hoverSprite = null; // 스프라이트 대신 알파로만 호버 효과를 낸다
                hoverEffect.hoverAlpha = unlockedHoverAlpha;
            }

            // stateImage 스프라이트가 방금 바뀌었으니, 호버가 끝났을 때 되돌아갈 기준값도 다시 잡는다.
            hoverEffect.RefreshNormalState();
        }
    }

    // 잠긴 스테이지는 button.interactable이 false라 애초에 클릭 이벤트가 오지 않는다.
    private void OnClickButton()
    {
        PlaySound(clickSound);
        UINavigator.Instance.OpenStageScene(stageNumber);
    }

    // 잠긴 스테이지는 button.interactable이 false여도 포인터 이벤트 자체는 들어오므로, 여기서 직접 걸러낸다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            PlaySound(hoverSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip, SoundSettings.SfxVolume);
    }
}
