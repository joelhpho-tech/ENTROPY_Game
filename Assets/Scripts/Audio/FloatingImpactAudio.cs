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
    private float velocityChangeSoftFloor;
    [SerializeField]
    private float velocityChangeHardFloor;
    [SerializeField]
    private float minPitchShift;
    [SerializeField]
    private float maxPitchShift;

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
        //this will make the volume change depending on velocity
        //volume (on an audio source) does not go higher than 1, which is normal volume, so the 'default' is it is quieter than normal.
        floatingAudioSource.volume = velocity;

        //if the bump sound is not on cool down
        if (!bumpSoundOnCoolDown)
        {
            //if the object experiences a change in velocity extreme enough to play a hard bounce sound...
            if (velocityDiff > velocityChangeHardFloor)
            {
                randomizeAudio(); //randomize the pitch between the min and max values
                floatingAudioSource.clip = hardBounce; //set sound to play to be the hard bounce sound
                floatingAudioSource.Play(); //play the sound
                StartCoroutine(bumpCooldown()); //dont play another sound for a bit
            }
            else if (velocityDiff > velocityChangeSoftFloor) //if the change is only enough to play a soft bounce sound...
            {
                randomizeAudio(); //randomize the pitch between the min and max values
                floatingAudioSource.clip = softBounce; //set sound to play to be the hard bounce sound
                floatingAudioSource.Play(); //play the sound
                StartCoroutine(bumpCooldown()); //dont play another sound for a bit
            }
        }

        //make sure this happens at the very end of FixedUpdate, or at minimum after it is checked against
        oneFrameBackVelocity = rb.linearVelocity.magnitude; //replace with velocity?
    }

    private void randomizeAudio()
    {
        var randPitch = Random.Range(minPitchShift, maxPitchShift);
        floatingAudioSource.pitch = randPitch;
    }

    private IEnumerator bumpCooldown()
    {
        bumpSoundOnCoolDown = true;
        yield return new WaitForSeconds(0.05f);
        bumpSoundOnCoolDown = false;
    }

    //this function is not called within the script, it is called in the logic for throwing the object
    public IEnumerator unmuteAfterTime()
    {
        yield return new WaitForSeconds(0.2f);
        floatingAudioSource.mute = false;
    }
}
