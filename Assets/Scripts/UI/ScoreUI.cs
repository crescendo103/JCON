using UnityEngine;
using UnityEngine.UI;
using DG;
using DG.Tweening;

public class ScoreUI : MonoBehaviour
{
    [Header("숫자 스프라이트 0~9 (순서대로 10개)")]
    public Sprite[] digitSprites; // digitSprites[0] = "0" 이미지, digitSprites[9] = "9" 이미지

    [Header("5자리를 표시할 이미지 슬롯 (왼쪽 -> 오른쪽 순서)")]
    public Image[] digitImages; // 길이 5

    [Header("시간당 획득 점수 설정")]
    public int scorePerSecond = 2; // 1초마다 오르는 점수
    public float scoreInterval = 1f; // 몇 초마다 점수를 올릴지

    private int currentScore = 0;
    private float timer = 0f;

    void Start()
    {
        // 시작할 때 00000으로 초기화
        UpdateDigits(0);

        //DOTween ddd;
        //ddd.

    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= scoreInterval)
        {
            timer -= scoreInterval;
            AddScore(scorePerSecond);
        }
    }

    // 외부에서 점수를 추가할 때 호출하는 함수
    // 나중에 몬스터 처치 시: scoreDisplay.AddScore(10); 같은 식으로 호출
    public void AddScore(int amount)
    {
        currentScore += amount;
        currentScore = Mathf.Clamp(currentScore, 0, 99999);
        UpdateDigits(currentScore);
    }

    public int GetScore()
    {
        return currentScore;
    }

    void UpdateDigits(int number)
    {
        number = Mathf.Clamp(number, 0, 99999);

        int[] digits = new int[5];
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            digits[i] = number % 10;
            number /= 10;
        }

        for (int i = 0; i < digitImages.Length; i++)
        {
            digitImages[i].sprite = digitSprites[digits[i]];
        }
    }
}