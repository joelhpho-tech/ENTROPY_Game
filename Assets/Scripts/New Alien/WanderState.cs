// WanderState.cs
using UnityEngine;

public class WanderState : AlienState
{
    public WanderState(AlienAI alien, AlienStateMachine stateMachine) : base(alien, stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entered WANDER state");

        // Pick a random connected node to start wandering
        if (alien.currentNode != null && alien.currentNode.neighbors.Count > 0)
        {
            alien.targetNode = alien.currentNode.neighbors[Random.Range(0, alien.currentNode.neighbors.Count)];
        }
        else
        {
            Debug.LogWarning("Alien has no current node or no neighbors!");
        }
    }

    public override void FrameUpdate()
    {
        // If we have a target node, move toward it
        if (alien.targetNode != null)
        {
            // Move toward target node
            alien.MoveTowards(alien.targetNode.transform.position, alien.wanderSpeed);

            // Rotate to face target node
            alien.RotateTowards(alien.targetNode.transform.position);

            // Check if we've reached the node
            float distanceToNode = Vector3.Distance(
                alien.transform.position,
                alien.targetNode.transform.position
            );

            if (distanceToNode < alien.nodeReachThreshold)
            {
                // We reached it! Update current node
                alien.currentNode = alien.targetNode;

                // Pick a new random neighbor to go to
                if (alien.currentNode.neighbors.Count > 0)
                {
                    alien.targetNode = alien.currentNode.neighbors[Random.Range(0, alien.currentNode.neighbors.Count)];
                    Debug.Log("Reached node, moving to: " + alien.targetNode.name);
                }
            }
        }
    }

    public override void Exit()
    {
        //Debug.Log("Exited WANDER state");
    }
}