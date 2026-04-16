using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioSwitcher : MonoBehaviour
{

    public AudioClip[] clips;
    private AudioSource src;


    // Start is called before the first frame update
    void Start()
    {
        src = GetComponent<AudioSource>();  
    }

    public void ChangeMusic(string location)
    {
        src.Stop();
        if (location.Equals("outside"))
        {
            src.clip = clips[0];
        }
        if (location.Equals("inside"))
        {
            src.clip = clips[1];
        }
        if (location.Equals("underground"))
        {
            src.clip = clips[2];
        }
        src.Play();
    }


}
