using UnityEngine;

public class PersistantManager : MonoBehaviour
{

    public static PersistantManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // destroy the Level2 duplicate
        }
    }
}
