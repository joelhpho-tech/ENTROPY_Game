using UnityEngine;
using System.Collections.Generic;
using System;

public class CheckpointManager : MonoBehaviour, ISaveable
{
    [Tooltip("Ordered list of your checkpoints in scene")]
    [SerializeField]
    public List<Checkpoint> checkpoints;
    public ZeroGravity playerZeroG;    // drag your player�s ZeroGravity component here

    int _currentIndex = 0;

    void Start()
    {
        // Wire up each checkpoint and only enable the first one
        for (int i = 0; i < checkpoints.Count; i++)
        {
            var cp = checkpoints[i];
            cp.OnReached += HandleCheckpointReached;
            cp.Initialize(playerZeroG, i == 0);
        }
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    void HandleCheckpointReached(Checkpoint reached)
    {
        // advance to next checkpoint if there is one
        if (_currentIndex + 1 < checkpoints.Count)
        {
            _currentIndex++;
            checkpoints[_currentIndex].Initialize(playerZeroG, true);
        }
        // store the Player's data to the save manager, passing in the position of this checkpoint
        playerZeroG.StorePlayerData(reached.respawnPoint.transform.position);
        // save the game at checkpoints
        GlobalSaveManager.SavedWithTerminal = false;
        GlobalSaveManager.SaveGame(false);
    }

    // these are for serialization and will be created during the save
    [Serializable]
    public class CheckPointData
    {
        [SerializeField]
        private List<bool> checkpointStates;
        public List<bool> CheckpointStates
        {
            get { return checkpointStates; }
        }
        public CheckPointData(List<bool> _checkpointStates)
        {
            checkpointStates = _checkpointStates;
        }
    }

    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            CheckPointData _checkpointData = JsonUtility.FromJson<CheckPointData>(loadedData);
            for (int i = 0; i < _checkpointData.CheckpointStates.Count; i++)
            {
                checkpoints[i].Col.enabled = _checkpointData.CheckpointStates[i];
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // store a copy of the checkpoint data in the global save manager
        // GlobalSaveManager.Instance.Data.Checkpoints = new List<Checkpoint>(checkpoints);
        CheckPointData _checkpointData = new CheckPointData(new List<bool>());
        foreach (Checkpoint _checkpoint in checkpoints)
        {
            _checkpointData.CheckpointStates.Add(_checkpoint.Col.enabled);
        }
        // this will create a file backing up the data we give it
        string json = JsonUtility.ToJson(_checkpointData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }
}
