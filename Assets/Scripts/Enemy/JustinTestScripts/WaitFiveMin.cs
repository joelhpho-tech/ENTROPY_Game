using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitFiveMin : MonoBehaviour
{
    [SerializeField] private GameObject giest2PreFab;

    private GameObject SpawnedGiest;

    [SerializeField] private IEnumerator giestSpawnWait;

    [SerializeField] private List<Waypoint> SpawnLocations;
    [SerializeField] private float waitTimer;
    [SerializeField] private Transform LevelWaypointGroup;
    [SerializeField] private Waypoint startingWaypoint;

    [SerializeField] private ZeroGravity zeroGravity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        giestSpawnWait = StartCountdown(waitTimer);

        StartCoroutine(giestSpawnWait);
    }

    private void SpawnGiest()
    {
        SpawnedGiest = Instantiate(giest2PreFab, (startingWaypoint = GetRandomSpawnWayPoint()).transform.position, Quaternion.identity, this.transform);

        if (SpawnedGiest == null) return; 

        SpawnedGiest.GetComponent<ComplexEnemyAI>().waypointGroup = LevelWaypointGroup;
        SpawnedGiest.GetComponent<ComplexEnemyAI>().startingWaypoint = startingWaypoint;
        //SpawnedGiest.GetComponent<ComplexEnemyAI>().playerController = zeroGravity;
    }

    private Waypoint GetRandomSpawnWayPoint()
    {
        return SpawnLocations[Random.Range(0, SpawnLocations.Count)];
    }

    private IEnumerator StartCountdown(float countdownTime)
    {
        float currentTime = countdownTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            yield return null; 
        }

        SpawnGiest();

        Debug.Log("Giest Has been Spawned");
    }
}
