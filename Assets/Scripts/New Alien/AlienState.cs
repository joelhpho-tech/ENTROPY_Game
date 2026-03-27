using UnityEngine;

public class AlienState
{
    protected AlienAI alien;
    protected AlienStateMachine stateMachine;

    // Constructor - called when creating each state
    public AlienState(AlienAI alien, AlienStateMachine stateMachine)
    {
        this.alien = alien;
        this.stateMachine = stateMachine;
    }

    // Virtual methods that states can override
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void FrameUpdate() { }
    public virtual void PhysicsUpdate() { }
}
