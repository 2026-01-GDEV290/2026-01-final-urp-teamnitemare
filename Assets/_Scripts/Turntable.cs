using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turntable : MonoBehaviour
{

    [SerializeField]
    [Range(-30, 30)] private float rotPerSecond = 10;
    private Vector3 rotation;

    private void Start()
    {
        rotation = new Vector3(0, rotPerSecond, 0);
    }


    // Update is called once per frame
    void Update()
    {
        rotation.y = rotPerSecond;

        transform.Rotate(rotation * Time.deltaTime);
    }
}
