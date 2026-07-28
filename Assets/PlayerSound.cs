using UnityEngine;
using MoreMountains.Feedbacks;

public class PlayerSound : MonoBehaviour
{
    public MMF_Player footstepSound;
    
    public void FootstepSoundPlay()
    {
        footstepSound.PlayFeedbacks();
    }
}
