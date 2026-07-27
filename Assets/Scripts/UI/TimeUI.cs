using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [Header("숫자 스프라이트 0~9 (순서대로 10개)")]
    public Sprite[] digitSprites; // digitSprites[0] = "0" 이미지, digitSprites[9] = "9" 이미지

    [Header("5자리를 표시할 이미지 슬롯 (왼쪽 -> 오른쪽 순서)")]
    public Image[] digitImages; // 길이 5, 예: 만/천/백/십/일의 자리

    private float elapsedTime = 0f;
    void Start()
    {
        // 시작할 때 00000으로 초기화
        UpdateDigits(0);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        UpdateDigits(totalSeconds);
    }

    void UpdateDigits(int number)
    {
        // 5자리로 제한 (00000 ~ 99999)
        number = Mathf.Clamp(number, 0, 99999);

        // 각 자리 숫자 추출
        int[] digits = new int[5];
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            digits[i] = number % 10;
            number /= 10;
        }

        // 이미지에 반영
        for (int i = 0; i < digitImages.Length; i++)
        {
            digitImages[i].sprite = digitSprites[digits[i]];
        }
    }
}