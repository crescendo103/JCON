using TMPro;
using UnityEngine;

// Banner 프리팹의 텍스트를 현재 스테이지 번호로 채운다. 페이드 인/아웃과 파괴는 이미
// BannerRevealUI가 전담하므로, 이 스크립트는 텍스트 내용만 책임진다.
// Awake에서 채우기 때문에(같은 오브젝트가 활성화될 때 모든 컴포넌트의 Awake가 OnEnable보다
// 먼저 실행되는 유니티 규칙 덕분에) BannerRevealUI.OnEnable()이 페이드인을 시작하기 전에
// 텍스트가 이미 채워져 있는 게 보장된다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class StageBannerText : MonoBehaviour
{
    [SerializeField] private string format = "STAGE {0}";

    private void Awake()
    {
        GetComponent<TextMeshProUGUI>().text = string.Format(format, StageProgressManager.Instance.CurrentStage);
    }
}
