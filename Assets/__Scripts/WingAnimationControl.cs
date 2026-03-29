using UnityEngine;

public class WingAnimationControl : MonoBehaviour
{
    private Animator wingAnimator = null;

    void Awake()
    {
        wingAnimator = GetComponentInChildren<Animator>();
        if (!wingAnimator)
            Debug.LogError("WingAnimationControl: No Animator found in children.");
    }

    public void SwitchAnimation(int index)
    {
        wingAnimator.SetInteger("Mode", index);
    }

    public void WingFlap()
    {
        wingAnimator.SetTrigger("FlapTrigger");
        //wingAnimator.SetInteger("Mode", 3);
        // Cancel any pending reset to idle to avoid conflicts.
        //CancelInvoke(nameof(ResetToIdle));        
        //Invoke(nameof(ResetToIdle), 1f);
    }
    public void ResetToIdle()
    {
        //wingAnimator.SetInteger("Mode", 0);
        wingAnimator.CrossFade("Idle", 0f);
    }

}
