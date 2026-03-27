using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Audio;

public class EnvironmentAudio : MonoBehaviour
{
    public GameObject doorsContainer;
    public HazardLight hazardLight;
    private DoorScript[] doors;
    private string doorContainerName = "DoorGroup";

    [Header("SFX Clips")]
    public AudioClip doorOpenClick;
    public AudioClip doorMoving;
    public AudioClip doorClosingClick;



    [Header("Audio Mixer Groups")]
    public AudioMixerGroup environmentGroup;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(doorsContainer == null)
        {
            doorsContainer = GameObject.Find(doorContainerName);
        }
        doors = doorsContainer.GetComponentsInChildren<DoorScript>();

        foreach (DoorScript door in doors)
        {
            door.startAudioSource.outputAudioMixerGroup = environmentGroup;
            door.middleAudioSource.outputAudioMixerGroup = environmentGroup;
            door.endAudioSource.outputAudioMixerGroup = environmentGroup;
            door.audioManager = this;
        }
    }

/*    public void PlayOneShotOf(AudioClip clip);
    {
        
    }*/



    /*
    public void playDoorOpenAudio(float speed, DoorScript door)
    {
        StartCoroutine(doorOpen(speed, door));
    }

    
    private IEnumerator doorOpen(float speed, DoorScript door)
    {
        AudioSource source = door.audioSource;
        source.PlayOneShot(doorOpenClick);
        source.clip = doorMoving;
        source.pitch = 1f + (speed / 5f) * 0.3f;
        source.Play();

        yield return new WaitUntil(() => door.DoorState == DoorScript.States.Open);

        source.Stop();
        source.PlayOneShot(doorClosingClick);
        
    }
    */
}
