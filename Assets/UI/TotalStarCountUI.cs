using TMPro;
using UnityEngine;

/// <summary>
/// 지금까지 모은 별 / 게임 전체 별 개수를 TMP 텍스트에 "6/54" 형태로 표시한다.
/// StageProgressManager의 진행도가 바뀔 때마다 이벤트를 받아 자동으로 새로고침된다.
/// TextMeshProUGUI가 붙어 있는 오브젝트에 함께 붙인다.
/// </summary>
public class TotalStarCountUI : MonoBehaviour
{
    [Header("표시 형식")]
    [Tooltip("{0} = 지금까지 모은 별, {1} = 게임 전체 별 개수. 예) \"{0}/{1}\" -> 6/54")]
    public string format = "{0}/{1}";

    private TextMeshProUGUI starText;

    private void Awake()
    {
        starText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        StageProgressManager.Instance.OnProgressChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        // Instance를 직접 쓰면 종료 중에 매니저를 다시 만들어버릴 수 있으므로 HasInstance로만 확인한다.
        if (StageProgressManager.HasInstance)
            StageProgressManager.Instance.OnProgressChanged -= Refresh;
    }

    /// <summary>현재 진행도로 텍스트를 갱신한다. 필요하면 외부에서 직접 호출해도 된다.</summary>
    public void Refresh()
    {
        if (starText == null)
            return;

        int total = StageProgressManager.Instance.TotalStars;
        int max = StageProgressManager.MaxTotalStars;
        starText.text = string.Format(format, total, max);
    }
}
