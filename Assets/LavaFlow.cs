using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaFlow : MonoBehaviour
{
    public Material mat;
    public float rate = 0.1f;
    private float textureOffset;
    Vector2 offsetV2 = Vector2.zero;

    Vector2 offsetMap2 = Vector2.zero;

    // Update is called once per frame
    void Update()
    {
        //float textureOffset = offset * Time.deltaTime;
         offsetV2 += Vector2.up * rate * Time.deltaTime;
        offsetMap2 += Vector2.right * rate * Time.deltaTime;


        mat.SetTextureOffset("_MainTex", offsetV2);
        mat.SetTextureOffset("_DetailAlbedoMap", offsetMap2);
    }
}
