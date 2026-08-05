using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드 설정 캔버스(배경음악/효과음 슬라이더 2개)를 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// 슬라이더 값은 SoundSettings(PlayerPrefs로 저장되는 전역 볼륨)를 그대로 읽고 쓴다 —
/// 여기서 값을 바꾸면 SoundManager(BGM)/ButtonStateEffect(효과음)가 즉시 반영한다.
/// BackButton은 씬 전환 없이 이 캔버스만 끈다(GiveUpConfirmCanvas의 NoButton과 같은 방식) —
/// 오버레이로 띄워 쓰는 캔버스라 어디서 열었든 그 화면으로 그대로 돌아간다.
/// SoundSettingsCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class SoundSettingsUI : MonoBehaviour
{
    private void Awake()
    {
        Slider bgmSlider = FindSlider("BgmSlider");
        Slider sfxSlider = FindSlider("SfxSlider");
        Button backButton = FindButton("BackButton");

        if (bgmSlider != null)
        {
            bgmSlider.value = SoundSettings.BgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundSettings.SfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);
    }

    // 하위 계층이 몇 단계든 상관없이 이름으로 슬라이더를 찾는다.
    private Slider FindSlider(string sliderName)
    {
        foreach (Slider slider in GetComponentsInChildren<Slider>(true))
        {
            if (slider.name == sliderName)
                return slider;
        }
        return null;
    }

    // 하위 계층이 몇 단계든 상관없이 이름으로 버튼을 찾는다.
    private Button FindButton(string buttonName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
                return button;
        }
        return null;
    }

    // 씬 전환 없이 이 캔버스만 닫는다.
    private void OnClickBack()
    {
        gameObject.SetActive(false);
    }

    private void OnBgmSliderChanged(float value)
    {
        SoundSettings.BgmVolume = value;
    }

    private void OnSfxSliderChanged(float value)
    {
        SoundSettings.SfxVolume = value;
    }
}
