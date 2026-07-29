using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [Header("숫자 스프라이트 0~9 (순서대로 10개)")]
    public Sprite[] digitSprites; // digitSprites[0] = "0" 이미지, digitSprites[9] = "9" 이미지

    [Header("5자리를 표시할 이미지 슬롯 (왼쪽 -> 오른쪽 순서)")]
    public Image[] digitImages; // 길이 5, 예: 만/천/백/십/일의 자리

    [Header("타이머 설정")]
    public float startTime = 300f; // 시작 시간(초)

    private float remainingTime;

    void Start()
    {
        remainingTime = startTime;
        UpdateDigits(Mathf.CeilToInt(remainingTime));
    }

    void Update()
    {
        if (remainingTime <= 0f) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime < 0f)
            remainingTime = 0f;

        int displaySeconds = Mathf.CeilToInt(remainingTime);
        UpdateDigits(displaySeconds);

        if (remainingTime <= 0f)
        {
            OnTimeUp();
        }
    }

    void OnTimeUp()
    {
        // 시간이 다 됐을 때 처리 (게임오버, 레벨클리어 등)
        Debug.Log("시간 종료!");
    }

    void UpdateDigits(int number)
    {
        // 5자리로 제한 (00000 ~ 99999)
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

    // 외부에서 남은 시간을 초 단위 정수로 가져갈 때 사용 (TotalUI 등에서 참조)
    public int GetRemainingSeconds()
    {
        return Mathf.CeilToInt(remainingTime);
    }
}