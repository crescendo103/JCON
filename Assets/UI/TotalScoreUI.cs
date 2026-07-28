using UnityEngine;
using TMPro;
using System.Collections;

public class TotalScoreUI : MonoBehaviour
{
    [Header("애니메이션 설정")]
    public float countDuration = 1f; // 목표값까지 올라가는 데 걸리는 총 시간

    private TextMeshProUGUI scoreText;
    private ScoreUI scoreUI;
    private TimeUI timeUI;

    private int displayedValue = 0;
    private int targetValue = 0;

    void Awake()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // 이름으로 찾아서 연결
        scoreUI = GameObject.Find("ScoreUI").GetComponent<ScoreUI>();
        timeUI = GameObject.Find("TimeUI").GetComponent<TimeUI>();

        // 값을 가져와서 저장
        targetValue = scoreUI.GetScore() + timeUI.GetRemainingSeconds();

        scoreText.text = "0";
        StartCoroutine(CountUpToTarget());

        if(targetValue > 100)
        {//별 3개

        }else if(targetValue > 50)
        {//별 2개

        }
        else if(targetValue >30)
        {//별 1개

        }
        else
        {//별 0개

        }
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