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

        // 좀비를 전부 잡아 실제로 클리어했을 때만 점수/남은시간으로 별점을 매긴다.
        // 죽거나 시간 초과로 끝났으면 클리어가 아니므로, 남은 시간이 많이 남아있어도 별 0개로 취급한다.
        int starCount = StageManager.StageCleared ? GetStarCount(targetValue) : 0;
        PlayStars(starCount);

        // 이번 스테이지 결과(별 개수)를 진행도 매니저에 보고한다. 이전 기록보다 좋을 때만 저장된다.
        if (StageManager.StageCleared)
            StageProgressManager.Instance.ReportCurrentStageResult(starCount);

        // 별 3개(만점)로 깼을 때만 "다음 스테이지" 버튼을 보여준다.
        Button nextStageButton = FindButtonInRoot("playButton (1)");
        if (nextStageButton != null)
            nextStageButton.gameObject.SetActive(starCount >= StageProgressManager.StarsPerStage);
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

    private int GetStarCount(int value)
    {
        if (value > 100) return 3;
        else if (value > 50) return 2;
        else if (value > 30) return 1;
        else return 0;
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