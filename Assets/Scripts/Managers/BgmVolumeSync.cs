using UnityEngine;

/// <summary>
/// SoundManager를 거치지 않고 직접 배치된 배경음악 AudioSource(예: UIScene의 UIscenesound)에
/// SoundSettings.BgmVolume을 적용한다. 시작할 때 AudioSource에 원래 설정된 volume을 "기준 볼륨"으로
/// 기억해두고, 그 기준값에 슬라이더 비율을 곱해서 적용한다 — SoundManager.ApplyBgmVolume과 같은 방식.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmVolumeSync : MonoBehaviour
{
    private AudioSource audioSource;
    private float baseVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume;
        ApplyVolume(SoundSettings.BgmVolume);
        SoundSettings.BgmVolumeChanged += ApplyVolume;
    }

    private void OnDestroy()
    {
        SoundSettings.BgmVolumeChanged -= ApplyVolume;
    }

    private void ApplyVolume(float bgmVolume)
    {
        audioSource.volume = baseVolume * bgmVolume;
    }
}
