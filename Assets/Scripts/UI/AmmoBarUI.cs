using UnityEngine;
using UnityEngine.UI;

public class AmmoBarUI : MonoBehaviour
{
    [Header("탄약 이미지 10개")]
    public Image[] ammoImages; // 배열 길이 10

    //[Header("참조할 무기 스크립트")]
    //public WeaponController weapon; 아직없음

    void Update()
    {
        //UpdateAmmoBar(weapon.currentAmmo); 아직 없음
    }

    void UpdateAmmoBar(int currentAmmo)
    {
        // 10발 초과면 전부 표시, 10발 이하면 남은 개수만큼만 표시
        int activeCount = Mathf.Clamp(currentAmmo, 0, ammoImages.Length);

        for (int i = 0; i < ammoImages.Length; i++)
        {
            ammoImages[i].enabled = (i < activeCount);
        }
    }
}