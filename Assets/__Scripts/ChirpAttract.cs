using UnityEngine;

public class ChirpAttract : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] AudioClip attractionSound;

    AudioSource cachedAudioSource;

    void Awake()
    {
        cachedAudioSource = GetComponent<AudioSource>();
        if (cachedAudioSource == null)
        {
            cachedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        cachedAudioSource.playOnAwake = false;
        cachedAudioSource.loop = false;
        cachedAudioSource.spatialBlend = 1f;    // 3D sound
        cachedAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    public void PlaySound()
    {
        if (attractionSound != null)
        {
            cachedAudioSource.transform.position = transform.position;
            cachedAudioSource.PlayOneShot(attractionSound);
        }
    }
}
