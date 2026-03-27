public class AlienStateMachine
{
    public AlienState currentState { get; private set; }

    public void Initialize(AlienState startingState)
    {
        currentState = startingState;
        currentState.Enter();
    }

    public void ChangeState(AlienState newState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
