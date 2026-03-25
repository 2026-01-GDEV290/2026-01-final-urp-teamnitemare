using System.Collections;
using UnityEngine;

public class BalanceController : MonoBehaviour
{
    [SerializeField] Animator anim;
    float lean;           // current balance
    float leanVelocity;   // optional inertia
    float gustForce;
    float gustTimer;
    [SerializeField] float driftStrength = 1f;     // how strong the random drift is
    [SerializeField] float counterStrength = 2f;   // how strong the player counterbalance is
    [SerializeField] float damping = 0.9f;         // how quickly inertia dies down (0.9 = 10% reduction per second)
    [SerializeField] float failThreshold = 30f;    // how far you have to lean before you fail

    [SerializeField] RectTransform needle;
    [SerializeField] float needleMaxOffset = 200f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(GustRoutine());

    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1. Random drift
        float drift = Random.Range(-1f, 1f) * driftStrength;

        // 2. Player counterbalance
        float counter = -Input.GetAxis("Horizontal") * counterStrength;

        // 3. Wind gusts
        UpdateGust(dt);

        // 4. Combine forces
        float totalForce = drift + counter + gustForce;

        // 5. Apply inertia
        leanVelocity += totalForce * dt;
        leanVelocity *= damping;

        lean += leanVelocity * dt;

        // 6. UI + Animation
        UpdateUI();
        UpdateAnimation();

        // 7. Failure check
        if (Mathf.Abs(lean) >= failThreshold)
            Fail();
    }

    void Fail()
    {
        Debug.Log("Failed! Lean was: " + lean);
        // You can add failure logic here, like restarting the level or showing a game over screen.
        // For now, we'll just reset the lean for testing purposes.
        lean = 0f;
        leanVelocity = 0f;
    }



    void UpdateUI()
    {
        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        needle.anchoredPosition = new Vector2(normalized * needleMaxOffset, 0f);
    }


    public void TriggerGust(float strength, float duration)
    {
        gustForce = strength;
        gustTimer = duration;
    }

    void UpdateGust(float dt)
    {
        if (gustTimer > 0f)
        {
            gustTimer -= dt;
            leanVelocity += gustForce * dt;
        }
    }


    IEnumerator GustRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            float strength = Random.Range(-3f, 3f);
            float duration = Random.Range(0.5f, 1.5f);
            TriggerGust(strength, duration);
        }
    }


    void UpdateAnimation()
    {
        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        anim.SetFloat("LeanAmount", normalized);
    }


}
