using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.Shapes;

public class DoorManager : MonoBehaviour, ISaveable
{

    [SerializeField]
    ZeroGravity player;
    [SerializeField]
    private DoorScript[] doors;
    [SerializeField]
    private List<DoorScript> doorsInRange;

    public GameObject DoorUI = null;

    private GameObject currentSelectedDoor = null;

    [SerializeField]
    Material unlockedMaterial;
    [SerializeField]
    Material lockedMaterial;
    [SerializeField]
    Material brokenMaterial;

    [Header("Hologram Variables")]

    private float EmissiveMapIntensity = 1.0f;

    // locked colors
    [SerializeField]
    public Texture2D lockedTexture;

    public Color LockedLightColor { get { return new Color(4.24f, 0.36f, 0.3f, 1.0f) * EmissiveMapIntensity; } }
    public Color LockedHoloBackColor = new Color(0.58f, 0.1f, 0.08f, 1.0f);
    public Color LockedHoloTextColor = new Color(1.0f, 0.3f, 0.2f, 1.0f);

    // unlocked colors
    [SerializeField]
    public Texture2D unlockedTexture;

    public Color UnlockedLightColor { get { return new Color(0.5f, 1.0f, 1.12f, 1.0f) * EmissiveMapIntensity; } }
    public Color UnlockedHoloBackColor = new Color(0.05f, 0.19f, 0.3f, 1.0f);
    public Color UnlockedHoloTextColor = new Color(0.63f, 0.87f, 0.85f, 1.0f);

    // broken colors
    [SerializeField]
    public Texture2D warningTexture;

    public Color WarningLightColor { get { return new Color(4.2f, 0.43f, 0.1f, 1.0f) * EmissiveMapIntensity; } }
    public Color WarningHoloBackColor = new Color(0.44f, 0.26f, 0.16f, 1.0f);
    public Color WarningHoloTextColor = new Color(0.95f, 0.64f, 0.32f, 1.0f);

    public GameObject CurrentSelectedDoor
    {
        get { return currentSelectedDoor; }
        set { currentSelectedDoor = value; }
    }

    public Material UnlockedMaterial
    {
        get { return unlockedMaterial; }
    }

    public Material LockedMaterial
    {
        get { return lockedMaterial; }
    }

    public Material WarningMaterial
    {
        get { return brokenMaterial; }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(player == null)
        {
            player = FindFirstObjectByType<ZeroGravity>();
        }
        doors = transform.Find("DoorGroup").GetComponentsInChildren<DoorScript>();
        doorsInRange = new List<DoorScript>();
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    // Update is called once per frame
    void Update()
    {
        ScanDoors();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {

        if (currentSelectedDoor)
        {
            DoorScript ds = currentSelectedDoor.GetComponent<DoorScript>();

            if (ds.HasPermissionLevel && player.AccessPermissions[(int)ds.Permission])
            {
                currentSelectedDoor.GetComponent<DoorScript>().UseDoor();
            }
            else if (!ds.HasPermissionLevel)
            {
                currentSelectedDoor.GetComponent<DoorScript>().UseDoor();
            }


        }

    }
    // these are for serialization and will be created during the save
    [Serializable]
    public class DoorData
    {
        [SerializeField]
        private List<DoorScript.States> doorStates;
        public List<DoorScript.States> DoorStates
        {
            get { return doorStates; }
        }
        public DoorData(List<DoorScript.States> _doorStates)
        {
            doorStates = _doorStates;
        }
    }
    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            DoorData _doorData = JsonUtility.FromJson<DoorData>(loadedData);
            for (int i = 0; i < _doorData.DoorStates.Count; i++)
            {
                doors[i].ForceState(_doorData.DoorStates[i]);
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // store a copy of the checkpoint data in the global save manager
        DoorData _doorData = new DoorData(new List<DoorScript.States>());
        foreach (DoorScript _door in doors)
        {
            if (_door.DoorState == DoorScript.States.Opening)
            {
                _doorData.DoorStates.Add(DoorScript.States.Closed);
            } else
            {
                _doorData.DoorStates.Add(_door.DoorState);
            }
        }
        // this will create a file backing up the data we give it
        string json = JsonUtility.ToJson(_doorData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

    private void ScanDoors()
    {
        Collider[] doorParts = Physics.OverlapSphere(player.transform.position, 5.0f, LayerMask.GetMask("Door"));
        List<DoorScript> nearDoors = new List<DoorScript>();

        // adds new doors in range
        foreach (Collider collider in doorParts)
        {

            DoorScript door = collider.transform.parent.GetComponentInParent<DoorScript>();
            nearDoors.Add(door);

            if (!doorsInRange.Contains(door))
            {
                MeshRenderer[] mr = collider.transform.parent.GetComponentsInChildren<MeshRenderer>();

                doorsInRange.Add(door);

                if (door.hologramGroup.Length != 0)
                {
                    //Debug.Log("fade on");
                    // fade on
                    door.StartFade(0.0f, 1.5f);

                }
                

            }


        }

        //clean out doors not in range
        for (int i = 0; i < doorsInRange.Count; i++)
        {
            {
                if (!nearDoors.Contains(doorsInRange[i]))
                {
                    //fade out doors no longer in range. checks for if the hologram is already deactivated from being open
                    if (doorsInRange[i].hologramGroup != null && doorsInRange[i].hologramActive == true)
                    {
                        doorsInRange[i].StartFade(1.0f, 1.5f);
                    }
                    
                    doorsInRange.Remove(doorsInRange[i]);
                    i--;
                }
            }

        }
    }

    public bool DoorInRange(DoorScript door)
    {

        if (doorsInRange.Contains(door)) return true;

        return false;

    }

    
}
