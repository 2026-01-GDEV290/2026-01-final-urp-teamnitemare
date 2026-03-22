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
