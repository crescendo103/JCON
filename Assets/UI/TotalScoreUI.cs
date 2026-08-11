using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MoreMountains.Feedbacks;

public class TotalScoreUI : MonoBehaviour
{
    [Header("애니메이션 재생시간")]
    public float countDuration = 1f; // 이시간동안 애니메이션 재생

    [Header("스프라이트")]
    public Image[] starImages;      // 별 담을 이미지
    public Sprite emptyStarSprite;  // 빈별
    public Sprite fullStarSprite;   // 밝은별

    [Header("MMFPlayer(MMF)")]
    public MMF_Player starFeedbackPlayer; // 직접 끌어와 연결

    private TextMeshProUGUI scoreText;
    private ScoreUI scoreUI;
    private TimeUI timeUI;
    private int displayedValue = 0;
    private int targetValue = 0;

    void Awake()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
        if (starFeedbackPlayer == null)
            starFeedbackPlayer = GetComponent<MMF_Player>();

        // 이 화면은 Time.timeScale = 0(스테이지 종료) 상태에서 뜨는데, MMF_Player의 PlayerTimescaleMode는
        // 재생 시퀀스의 지연/쿨다운에만 적용되고, 별 하나하나가 움직이는 개별 Feedback은 각자의
        // Timing.TimescaleMode(기본값 Scaled)를 따로 따른다. ForceTimescaleMode를 켜야 하위 Feedback
        // 전부가 개별 설정과 무관하게 강제로 unscaled(실시간 기준)로 재생된다(사망/클리어 둘 다 동일한 원인).
        if (starFeedbackPlayer != null)
        {
            starFeedbackPlayer.PlayerTimescaleMode = TimescaleModes.Unscaled;
            starFeedbackPlayer.ForceTimescaleMode = true;
            starFeedbackPlayer.ForcedTimescaleMode = TimescaleModes.Unscaled;
        }
    }

    void Start()
    {
        GameObject scoreUIObj = GameObject.Find("scoretext");
        GameObject timeUIObj = GameObject.Find("timertext");
        scoreUI = scoreUIObj != null ? scoreUIObj.GetComponent<ScoreUI>() : null;
        timeUI = timeUIObj != null ? timeUIObj.GetComponent<TimeUI>() : null;

        if (scoreUI == null || timeUI == null)
        {
            targetValue = 100;
        }
        else
        {
            targetValue = scoreUI.GetScore() + timeUI.GetRemainingSeconds();
        }

        ResetStarsToEmpty();

        scoreText.text = "0";
        StartCoroutine(CountUpToTarget());

        // 죽지 않고 좀비를 전부 잡아 실제로 클리어했으면 무조건 별 3개(만점), 그 외(사망/시간초과)는 별 0개.
        int starCount = StageManager.StageCleared ? StageProgressManager.StarsPerStage : 0;
        PlayStars(starCount);

        // 이번 스테이지 결과(별 개수)를 진행도 매니저에 보고한다. 이전 기록보다 좋을 때만 저장된다.
        // ScoreReported로 한 번만 보고되게 잠가서, 스크립트 재컴파일 등으로 이 Start()가 예외적으로
        // 두 번 실행되더라도 같은 클리어가 두 번 보고돼 다음 스테이지에 별이 잘못 기록되지 않게 한다.
        if (StageManager.StageCleared && !StageManager.ScoreReported)
        {
            StageManager.ScoreReported = true;
            StageProgressManager.Instance.ReportCurrentStageResult(starCount);
        }

        // 별 3개(만점)로 깼을 때만 "다음 스테이지" 버튼을 보여준다.
        Button nextStageButton = FindButtonInRoot("playButton (1)");
        if (nextStageButton != null)
            nextStageButton.gameObject.SetActive(starCount >= StageProgressManager.StarsPerStage);

        // completeText/scoretext는 Content Size Fitter로 자기 크기를 정하고, 그 위의 Vertical Layout
        // Group(Text 오브젝트)이 그 크기를 보고 간격(Spacing)을 잡는다. Instantiate 직후 첫 프레임에는
        // Content Size Fitter가 아직 실제 크기를 계산하기 전에 Layout Group이 먼저 배치해버려서
        // 간격이 어긋난 채로 나온다(인스펙터에서 값을 살짝 건드리면 강제로 다시 배치되어 바로 고쳐지는
        // 것과 같은 증상). 초기 상태를 다 세팅한 직후 여기서 강제로 한 번 더 재배치해서 같은 효과를 낸다.
        LayoutGroup layoutGroup = GetComponentInParent<LayoutGroup>();
        if (layoutGroup != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
    }

    // 하위 계층이 몇 단계든 상관없이 이름으로 버튼을 찾는다.
    private Button FindButtonInRoot(string buttonName)
    {
        foreach (Button button in transform.root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
                return button;
        }
        return null;
    }

    private void ResetStarsToEmpty()
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
                starImages[i].sprite = emptyStarSprite;
        }
    }

    private void PlayStars(int starCount)
    {
        if (starFeedbackPlayer == null) return;

        // 스프라이트만 개수에 맞게 채우고, 애니메이션은 3개 다 재생
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
                starImages[i].sprite = (i < starCount) ? fullStarSprite : emptyStarSprite;
        }

        starFeedbackPlayer.PlayFeedbacks();
    }

    private IEnumerator CountUpToTarget()
    {
        if (targetValue <= 0)
        {
            scoreText.text = "0";
            yield break;
        }
        float interval = countDuration / targetValue;
        while (displayedValue < targetValue)
        {
            displayedValue++;
            scoreText.text = displayedValue.ToString();
            // Time.timeScale이 0(스테이지 종료 상태)이어도 결과 화면 연출은 계속 진행돼야 하므로 실시간 대기를 쓴다.
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}