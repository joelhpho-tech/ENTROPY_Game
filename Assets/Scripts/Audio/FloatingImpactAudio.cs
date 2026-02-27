using UnityEngine;
using System.Collections;

public class FloatingImpactAudio : MonoBehaviour
{
    public AudioSource floatingAudioSource;
    Rigidbody rb;
    private bool bumpSoundOnCoolDown = false;
    private float velocity;
    private float oneFrameBackVelocity = 0;
    private float velocityDiff;
    [SerializeField]
    private float velocityChangeFloor;
    [SerializeField]
    private float velocityChangeHardFloor;

    [Header("Wall Bounce SFX")]
    public AudioClip softBounce;
    public AudioClip hardBounce;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
        floatingAudioSource = this.gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        velocity = rb.linearVelocity.magnitude;
        velocityDiff = Mathf.Abs(velocity - oneFrameBackVelocity);

        //if the bump sound is not on cool down
        if (!bumpSoundOnCoolDown)
        {
            //if the object experiences a change in velocity extreme enough to play a hard bounce sound...
            if (velocityDiff > velocityChangeHardFloor)
            {
                Debug.Log(velocityDiff);
                floatingAudioSource.clip = hardBounce;
                floatingAudioSource.Play();
                StartCoroutine(bumpCooldown());
            }
            else if (velocityDiff > velocityChangeFloor) //if the change is only enough to play a soft bounce sound...
            {
                Debug.Log(velocityDiff);
                floatingAudioSource.clip = softBounce;
                floatingAudioSource.Play();
                StartCoroutine(bumpCooldown());
            }
        }

        //make sure this happens at the very end of FixedUpdate, or at minimum after it is checked against
        oneFrameBackVelocity = rb.linearVelocity.magnitude;
    }

    private IEnumerator bumpCooldown()
    {
        bumpSoundOnCoolDown = true;
        yield return new WaitForSeconds(0.2f);
        bumpSoundOnCoolDown = false;
    }

    public IEnumerator unmuteAfterTime()
    {
        yield return new WaitForSeconds(0.2f);
        floatingAudioSource.mute = false;
    }
}
