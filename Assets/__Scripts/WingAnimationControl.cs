using UnityEngine;

public class WingAnimationControl : MonoBehaviour
{
    private Animator wingAnimator = null;
    [SerializeField] private AlphaControllerForAnimationRenderer wingsAlphaControl;

    void Awake()
    {
        wingAnimator = GetComponentInChildren<Animator>();
        if (!wingAnimator)
            Debug.LogError("WingAnimationControl: No Animator found in children.");
    }
    void Start()
    {
        if (wingsAlphaControl)
        {
            wingsAlphaControl.SetFullyTransparent();
            wingsAlphaControl.SetEmissionIntensity(0f);
        }
    }

    public void SwitchAnimation(int index)
    {
        wingAnimator.SetInteger("Mode", index);
    }

    public void WingFlap()
    {
        if (wingsAlphaControl)
        {
            wingsAlphaControl.SetToAlpha(1f);
            wingsAlphaControl.SetEmissionIntensity(1f);
        }
        wingAnimator.SetTrigger("FlapTrigger");
        wingsAlphaControl.FadeOutToTransparent(1f);
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
