using System;
using UnityEngine;

[Serializable]
public class TimerObject : MonoBehaviour
{
    [SerializeField] private bool isEnabled = true;
    public float timerDuration = 5f;
    public bool restartAfterExpire = false;

    public UnityEngine.Events.UnityEvent onTimerExpire;

    public void StartTimer()
    {
        if (!isEnabled)
            return;
        Invoke(nameof(TimerExpired), timerDuration);
    }

    public void CancelTimer()
    {
        CancelInvoke(nameof(TimerExpired));
    }

    public void SetIsEnabled(bool value)
    {
        isEnabled = value;
        if (!isEnabled)
        {
            CancelTimer();
        }
    }

    public bool ResetTimer()
    {
        if (!isEnabled)
            return false;
        CancelTimer();
        StartTimer();
        return true;
    }

    public bool SetTimerDuration(float duration)
    {
        if (duration <= 0f)
            return false;
        // safeguard if actively running
        CancelTimer();
        timerDuration = duration;
        return true;
    }

    private void TimerExpired()
    {
        Debug.Log("Timer expired! on object: " + gameObject.name);
        onTimerExpire?.Invoke();

        if (restartAfterExpire)
        {
            StartTimer();
        }
    }
}
