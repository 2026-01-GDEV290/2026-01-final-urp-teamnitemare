using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections;

public class PlayerControllerBB : MonoBehaviour
{
    private CharacterController characterController;
    public Transform head;
    public Camera playerCamera;

    private Transform weapon;
    private Collider weaponCollider;
    //private BrickBarrier brickBarrier;

    bool attacking = false;
    bool retracting = false;

    public float moveSpeed = 5f;
    public float rotateSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -30f;

    public float swingSpeed = 250f;

    [SerializeField] private float cameraShakeDuration = 0.1f;
    [SerializeField] private float cameraShakeStrength = 0.15f;

    private Vector3 cameraOriginalPosition;
    private float rotationX;
    private float rotationY;
    private float verticalVelocity;

    private int hitCount = 0;

    public Vector3 newVelocity;

    [SerializeField] private GameObject dustAirEffectPrefab;
    [SerializeField] private GameObject explosion01EffectPrefab;

    [SerializeField] AudioClip weaponSwingSound;
    [SerializeField] AudioClip brickHitSound;
    [SerializeField] AudioClip brickExplodeSound;
    [SerializeField] AudioClip brickSlideSound;


    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rotationY = transform.localEulerAngles.y;
        rotationX = transform.localEulerAngles.x;
        //brickBarrier = FindFirstObjectByType<BrickBarrier>();
    }

    void OnEnable()
    {
        MeleeWeapon.OnHit += MeleeHit;
    }

    void OnDisable()
    {
        MeleeWeapon.OnHit -= MeleeHit;
    }

    void Start()
    {
        //weapon = GameObject.Find("SledgePrimitive").transform;
        weapon = GameObject.Find("SledgeHammer").transform;
        //set collider to isTrigger
        weaponCollider = weapon.GetComponent<Collider>();
        weaponCollider.isTrigger = true;

    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        //rb.linearVelocity = transform.TransformDirection(newVelocity);
        if (attacking)
        {
            //if (weapon.rotation.eulerAngles.x >= 90f)
            if (weapon.localRotation.eulerAngles.z >= 45f)
            {
                attacking = false;
                retracting = true;
                weapon.localRotation = Quaternion.Euler(weapon.localRotation.eulerAngles.x, weapon.localRotation.eulerAngles.y, 45f);
                weaponCollider.isTrigger = true;
            }
            else
                weapon.Rotate(Vector3.forward * swingSpeed * Time.fixedDeltaTime, Space.Self);
        }
        else if (retracting)
        {
            //Debug.Log("Retracting, angle: " + weapon.rotation.eulerAngles.x);
            //if (weapon.rotation.eulerAngles.x <= 0f || weapon.rotation.eulerAngles.x >= 180f)
            if (weapon.localRotation.eulerAngles.z <= 0f || weapon.localRotation.eulerAngles.z >= 180f)
            {
                retracting = false;
                weapon.localRotation = Quaternion.Euler(weapon.localRotation.eulerAngles.x, weapon.localRotation.eulerAngles.y, 0);
            }
            else
                weapon.Rotate(Vector3.back * swingSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    void LateUpdate()
    {   
    }

    public void Move(Vector2 moveVector)
    {
        Vector3 move = transform.forward * moveVector.y + transform.right * moveVector.x;
        move = move * moveSpeed * Time.deltaTime;
        characterController.Move(move);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    public void Rotate(Vector2 lookVector)
    {
        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * Time.deltaTime;
        // make sure to clamp the x rotation to prevent flipping over
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);        
        rotationX -= lookVector.y * rotateSpeed * Time.deltaTime;
        //rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    public void Jump()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    public void Attack()
    {
        if (!attacking && !retracting)
        {
            attacking = true;
            weaponCollider.isTrigger = false;
            // play weapon swing sound
            if (weaponSwingSound != null)
            {
                AudioSource.PlayClipAtPoint(weaponSwingSound, weapon.position);
            }
        }
        //Debug.Log("Attack triggered");
    }



    public void MeleeHit(GameObject hitObject, Vector3 hitPoint)
    {
        Debug.Log("PlayerController registered melee hit on: " + hitObject.name);
        
        if (!retracting)
        {
            if (attacking)
            {
                if (hitObject.CompareTag("Brick"))
                {
                    hitCount++;
                    if (hitCount > 3)
                        hitCount = 0;
                    
                    cameraShakeDuration = Mathf.Clamp(hitCount * 0.33f, 0.33f, 2f);
                    cameraShakeStrength = Mathf.Clamp(hitCount * 0.05f, 0.05f, 0.3f);
                    StartCoroutine(CameraShake());
                    Debug.Log("Hit brick");

                    if (hitCount == 3)
                    {
                        // add FX_explosion_01 to hitPoint, scale 25%
                        GameObject explosion = Instantiate(explosion01EffectPrefab, hitPoint, Quaternion.identity);
                        ApplyParticleScale(explosion, 0.5f);
                        ApplyParticleTransparency(explosion, 0.75f);
                        ApplyParticleDuration(explosion, 0.10f);
                        
                        // play brick explode sound
                        if (brickExplodeSound != null)
                        {
                            AudioSource.PlayClipAtPoint(brickExplodeSound, hitPoint);
                        }

                        // Trigger brick barrier destruction or other effects
                        // if (brickBarrier != null)
                        // {
                        //     brickBarrier.BrickExplode(hitObject, hitPoint);
                        // }
                        // also slow down time for 2 seconds
                        Time.timeScale = 0.25f;
                        StartCoroutine(ResetTimeScale());
                    }
                    else
                    {
                        // add FX_spash_dust_air to hitPoint scale 25%
                        GameObject dustAir = Instantiate(dustAirEffectPrefab, hitPoint, Quaternion.identity);
                        ApplyParticleScale(dustAir, 0.33f);
                        ApplyParticleTransparency(dustAir, 0.75f);

                        // play brick hit sound
                        if (brickHitSound != null)
                        {
                            AudioSource.PlayClipAtPoint(brickHitSound, hitPoint);
                        }
                        if (brickSlideSound != null)
                        {
                            AudioSource.PlayClipAtPoint(brickSlideSound, hitPoint);
                        }
                        // Trigger brick bash response
                        // if (brickBarrier != null)
                        // {
                        //     brickBarrier.BrickBashRespond(hitObject, hitPoint);
                        // }
                    }
                }
                attacking = false;
                retracting = true;
                // Ignore further hits during retraction/after attack finished
                weaponCollider.isTrigger = true;
            }
        }
    }
#region Camera Shake
    private IEnumerator CameraShake()
    {
        cameraOriginalPosition = playerCamera.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < cameraShakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cameraShakeDuration;
            float intensity = (1f - t) * cameraShakeStrength; // Fade out the shake

            Vector3 randomShake = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * intensity;

            playerCamera.transform.localPosition = cameraOriginalPosition + randomShake;
            yield return null;
        }

        playerCamera.transform.localPosition = cameraOriginalPosition;
    }
#endregion Camera Shake

#region Time Scale Reset
    private IEnumerator ResetTimeScale()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
    }
#endregion Time Scale Reset

#region Particle FX Helpers
    private void ApplyParticleScale(GameObject fx, float scale)
    {
        if (fx == null) return;

        fx.transform.localScale *= scale;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Start Size
            main.startSizeMultiplier *= scale;

            // Size over Lifetime
            var sol = ps.sizeOverLifetime;
            if (sol.enabled)
                sol.sizeMultiplier *= scale;

            // Size by Speed
            var sbs = ps.sizeBySpeed;
            if (sbs.enabled)
                sbs.sizeMultiplier *= scale;

            // Shape (emission volume)
            var shape = ps.shape;
            shape.scale *= scale;
        }
    }

    private void ApplyParticleTransparency(GameObject fx, float alpha)
    {
        if (fx == null) return;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            Color startColor = main.startColor.color;
            startColor.a *= alpha; // Multiply existing alpha
            main.startColor = startColor;

            // If Color over Lifetime is enabled
            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                // Note: Cannot easily modify curves at runtime
                // Consider disabling or using Start Color only
            }
        }
    }
    private void ApplyParticleDuration(GameObject fx, float durationMultiplier)
    {
        if (fx == null) return;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            
            // Use Unscaled time so Time.timeScale doesn't affect particles
            main.simulationSpace = ParticleSystemSimulationSpace.World;
           // main.useUnscaledTime = true;
            
            main.duration *= durationMultiplier;
            main.startLifetimeMultiplier *= durationMultiplier;
            main.loop = false;
        }
    }
#endregion Particle FX Helpers
}