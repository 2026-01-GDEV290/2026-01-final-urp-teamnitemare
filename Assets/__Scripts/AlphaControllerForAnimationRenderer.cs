using UnityEngine;

[ExecuteAlways]
public class AlphaControllerForAnimationRenderer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float alpha = 1f;

    [Range(0f, 1f)]
    public float emissionMultiplier = 1f;

    Renderer r;
    MaterialPropertyBlock block;

    void Awake()
    {
        r = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        if (!r) r = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();
        Apply();
    }

    public void Apply()
    {
        if (!r) return;

        r.GetPropertyBlock(block);

        Debug.Log("Has _Color: " + block.HasColor("_Color"));
        Debug.Log("Has _BaseColor: " + block.HasColor("_BaseColor"));


        // Base color alpha
        if (block.HasColor("_BaseColor"))
        {
            Color c = block.GetColor("_BaseColor");
            c.a = alpha;
            block.SetColor("_BaseColor", c);
        }

        // Emission (Standard/URP)
        if (block.HasColor("_EmissionColor"))
        {
            Color e = block.GetColor("_EmissionColor");
            e *= emissionMultiplier;
            block.SetColor("_EmissionColor", e);
        }

        // Emission (HDRP)
        if (block.HasColor("_EmissiveColor"))
        {
            Color e = block.GetColor("_EmissiveColor");
            e *= emissionMultiplier;
            block.SetColor("_EmissiveColor", e);
        }

        r.SetPropertyBlock(block);
    }
}

