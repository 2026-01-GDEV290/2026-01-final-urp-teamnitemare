using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMusic : MonoBehaviour
{

    private GameObject Audiomanager;
    public string Changetoo;
    // Start is called before the first frame update
    void Start()
    {
        Audiomanager = GameObject.Find("Auido Manager");  
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        Audiomanager.GetComponent<audioSwitcher>().ChangeMusic(Changetoo);    
    }
}
