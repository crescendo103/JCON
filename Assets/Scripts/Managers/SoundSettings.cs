using System;
using UnityEngine;

/// <summary>
/// 배경음악(BGM)/효과음(SFX) 볼륨을 전역으로 들고 있는 정적 저장소.
/// PlayerPrefs에 저장/로드해서 앱을 다시 켜도 값이 유지된다.
/// 값이 바뀌면 이벤트로 알려주므로, 실제로 소리를 재생하는 쪽(SoundManager의 BGM AudioSource,
/// ButtonStateEffect의 효과음 등)이 구독해서 즉시 반영한다. SoundSettingsUI(볼륨 슬라이더 캔버스)가
/// 이 값을 읽고 쓴다.
/// </summary>
public static class SoundSettings
{
    private const string BgmKey = "SoundSettings.BgmVolume";
    private const string SfxKey = "SoundSettings.SfxVolume";

    private static float bgmVolume = PlayerPrefs.GetFloat(BgmKey, 1f);
    private static float sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);

    public static event Action<float> BgmVolumeChanged;
    public static event Action<float> SfxVolumeChanged;

    public static float BgmVolume
    {
        get => bgmVolume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (clamped == bgmVolume) return;

            bgmVolume = clamped;
            PlayerPrefs.SetFloat(BgmKey, bgmVolume);
            BgmVolumeChanged?.Invoke(bgmVolume);
        }
    }

    public static float SfxVolume
    {
        get => sfxVolume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (clamped == sfxVolume) return;

            sfxVolume = clamped;
            PlayerPrefs.SetFloat(SfxKey, sfxVolume);
            SfxVolumeChanged?.Invoke(sfxVolume);
        }
    }
}
