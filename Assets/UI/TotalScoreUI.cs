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

        int starCount = GetStarCount(targetValue);
        PlayStars(starCount);

        // 이번 스테이지 결과(별 개수)를 진행도 매니저에 보고한다. 이전 기록보다 좋을 때만 저장된다.
        StageProgressManager.Instance.ReportCurrentStageResult(starCount);
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
            yield return new WaitForSeconds(interval);
        }
    }
}