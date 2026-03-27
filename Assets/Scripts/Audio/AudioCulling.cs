using System.Linq;
using UnityEngine;

public class AudioCulling : MonoBehaviour
{
    public AudioSource[] exempt;
    public AudioSource[] cullable;
    [SerializeField] private AudioZone[] audioZones;
    [SerializeField] private GameObject player;

    public AudioZone currentZone;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cullable = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        audioZones = FindObjectsByType<AudioZone>(FindObjectsSortMode.None);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < audioZones.Length; i++)
        { //for every audio zone...
            // Skip destroyed zones
            if (audioZones[i] == null) continue;

            if (audioZones[i].bounds.Contains(player.transform.position))
            { //if the audio zone has the player in it...
                audioZones[i].Activate(); //activate all the audio sources in that zone
                currentZone = audioZones[i]; //and set the current zone to be this one, since it contains the player
                for (int j = 0; j < audioZones[i].adjacentZones.Length; j++)
                { //and, for every audio zone which is listed as adjacent to that audio zone...
                    audioZones[i].adjacentZones[j].Activate(); //activate the audio sources in those, too
                }
            }
            else
            { //however...
                if (currentZone != null)
                { //assuming that we have a current zone, we should
                    if (!currentZone.adjacentZones.Contains(audioZones[i]))
                    { //unless a zone is listed as adjacent to the current zone...
                        audioZones[i].Deactivate(); //deactivate all the audio sources in it
                    }
                }
            }
        }
    }
}
