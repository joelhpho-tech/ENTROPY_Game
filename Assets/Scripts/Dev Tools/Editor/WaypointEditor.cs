
// WaypointEditor.cs
// Place this script in an "Editor" folder in your project
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Waypoint))]
public class WaypointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Waypoint waypoint = (Waypoint)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Connect Tools", EditorStyles.boldLabel);

        // Distance threshold setting
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Max Distance:", GUILayout.Width(100));
        if (!EditorPrefs.HasKey("WaypointMaxDistance"))
            EditorPrefs.SetFloat("WaypointMaxDistance", 10f);
        float maxDistance = EditorPrefs.GetFloat("WaypointMaxDistance");
        maxDistance = EditorGUILayout.FloatField(maxDistance, GUILayout.Width(50));
        EditorPrefs.SetFloat("WaypointMaxDistance", maxDistance);
        EditorGUILayout.EndHorizontal();

        // Line of sight setting
        bool useLineOfSight = EditorPrefs.GetBool("WaypointUseLineOfSight", true);
        useLineOfSight = EditorGUILayout.Toggle("Use Line of Sight", useLineOfSight);
        EditorPrefs.SetBool("WaypointUseLineOfSight", useLineOfSight);

        if (useLineOfSight)
        {
            EditorGUILayout.HelpBox("Will only connect waypoints with clear line of sight (no obstacles).", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Connect this waypoint to nearby waypoints
        if (GUILayout.Button("Connect This Waypoint to Neighbors"))
        {
            Undo.RecordObject(waypoint, "Connect Waypoint to Neighbors");
            int connectionsAdded = ConnectWaypoint(waypoint, maxDistance, useLineOfSight);
            EditorUtility.SetDirty(waypoint);
            Debug.Log($"Connected {waypoint.name} to {connectionsAdded} neighbors.");
        }

        // Connect to nearest neighbors with adaptive distance
        if (GUILayout.Button("Connect to Nearest Neighbors (Auto-Distance)"))
        {
            Undo.RecordObject(waypoint, "Connect to Nearest Neighbors");
            int connectionsAdded = ConnectToNearestNeighbors(waypoint);
            EditorUtility.SetDirty(waypoint);
            Debug.Log($"Connected {waypoint.name} to {connectionsAdded} nearest neighbors.");
        }

        // Clear connections for this waypoint
        if (GUILayout.Button("Clear This Waypoint's Connections"))
        {
            Undo.RecordObject(waypoint, "Clear Waypoint Connections");
            int connectionsCleared = waypoint.neighbors.Count;

            // Remove this waypoint from all neighbors (bidirectional)
            foreach (Waypoint neighbor in waypoint.neighbors.ToList())
            {
                if (neighbor != null && neighbor.neighbors.Contains(waypoint))
                {
                    Undo.RecordObject(neighbor, "Clear Waypoint Connections");
                    neighbor.neighbors.Remove(waypoint);
                    EditorUtility.SetDirty(neighbor);
                }
            }

            waypoint.neighbors.Clear();
            EditorUtility.SetDirty(waypoint);
            Debug.Log($"Cleared {connectionsCleared} connections from {waypoint.name}.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Type Management", EditorStyles.boldLabel);

        // Change type buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set to General"))
        {
            Undo.RecordObject(waypoint, "Set Waypoint Type");
            waypoint.type = Waypoint.WaypointType.General;
            EditorUtility.SetDirty(waypoint);
            Debug.Log($"Set {waypoint.name} to General type.");
        }
        if (GUILayout.Button("Set to Roaming"))
        {
            Undo.RecordObject(waypoint, "Set Waypoint Type");
            waypoint.type = Waypoint.WaypointType.Roaming;
            EditorUtility.SetDirty(waypoint);
            Debug.Log($"Set {waypoint.name} to Roaming type.");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene-Wide Tools", EditorStyles.boldLabel);

        // Connect all waypoints in scene
        if (GUILayout.Button("Connect ALL Waypoints in Scene"))
        {
            if (EditorUtility.DisplayDialog("Connect All Waypoints",
                "This will connect all waypoints in the scene based on distance and line of sight. Continue?",
                "Yes", "Cancel"))
            {
                ConnectAllWaypoints(maxDistance, useLineOfSight);
            }
        }

        // Clear all connections in scene
        if (GUILayout.Button("Clear ALL Waypoint Connections in Scene"))
        {
            if (EditorUtility.DisplayDialog("Clear All Connections",
                "This will remove all connections between waypoints in the scene. This cannot be easily undone. Continue?",
                "Yes", "Cancel"))
            {
                ClearAllConnections();
            }
        }
    }

    private int ConnectWaypoint(Waypoint waypoint, float maxDistance, bool useLineOfSight)
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();
        int connectionsAdded = 0;

        foreach (Waypoint other in allWaypoints)
        {
            // Skip self
            if (other == waypoint) continue;

            // Skip if already connected
            if (waypoint.neighbors.Contains(other)) continue;

            // Check distance
            float distance = Vector3.Distance(waypoint.transform.position, other.transform.position);
            if (distance > maxDistance) continue;

            // Check line of sight if enabled
            if (useLineOfSight)
            {
                Vector3 direction = other.transform.position - waypoint.transform.position;
                Ray ray = new Ray(waypoint.transform.position, direction);

                if (Physics.Raycast(ray, out RaycastHit hit, distance))
                {
                    // If we hit something other than the target waypoint, there's an obstacle
                    if (hit.collider.gameObject != other.gameObject)
                        continue;
                }
            }

            // Add bidirectional connection
            Undo.RecordObject(waypoint, "Connect Waypoints");
            Undo.RecordObject(other, "Connect Waypoints");

            waypoint.neighbors.Add(other);
            if (!other.neighbors.Contains(waypoint))
            {
                other.neighbors.Add(waypoint);
            }

            EditorUtility.SetDirty(waypoint);
            EditorUtility.SetDirty(other);
            connectionsAdded++;
        }

        return connectionsAdded;
    }

    private int ConnectToNearestNeighbors(Waypoint waypoint)
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();

        // Create a list of all other waypoints with their distances
        List<(Waypoint waypoint, float distance)> waypointDistances = new List<(Waypoint, float)>();

        foreach (Waypoint other in allWaypoints)
        {
            // Skip self and already connected waypoints
            if (other == waypoint) continue;
            if (waypoint.neighbors.Contains(other)) continue;

            float distance = Vector3.Distance(waypoint.transform.position, other.transform.position);
            waypointDistances.Add((other, distance));
        }

        // If no waypoints to connect to, return
        if (waypointDistances.Count == 0)
        {
            Debug.Log($"{waypoint.name} has no unconnected waypoints nearby.");
            return 0;
        }

        // Sort by distance
        waypointDistances.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Get the nearest waypoint's distance
        float nearestDistance = waypointDistances[0].distance;

        // Calculate threshold (nearest distance + 20% tolerance)
        float distanceThreshold = nearestDistance * 1.2f;

        int connectionsAdded = 0;

        // Connect to all waypoints within the threshold
        foreach (var (other, distance) in waypointDistances)
        {
            if (distance > distanceThreshold)
                break; // Since list is sorted, we can stop here

            // Add bidirectional connection
            Undo.RecordObject(waypoint, "Connect to Nearest Neighbors");
            Undo.RecordObject(other, "Connect to Nearest Neighbors");

            waypoint.neighbors.Add(other);
            if (!other.neighbors.Contains(waypoint))
            {
                other.neighbors.Add(waypoint);
            }

            EditorUtility.SetDirty(waypoint);
            EditorUtility.SetDirty(other);
            connectionsAdded++;

            Debug.Log($"Connected {waypoint.name} to {other.name} (distance: {distance:F2})");
        }

        Debug.Log($"Distance threshold used: {distanceThreshold:F2} (based on nearest: {nearestDistance:F2})");
        return connectionsAdded;
    }

    private void ConnectAllWaypoints(float maxDistance, bool useLineOfSight)
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();
        int totalConnections = 0;

        EditorUtility.DisplayProgressBar("Connecting Waypoints", "Processing...", 0f);

        for (int i = 0; i < allWaypoints.Length; i++)
        {
            Waypoint waypoint = allWaypoints[i];
            EditorUtility.DisplayProgressBar("Connecting Waypoints",
                $"Processing {waypoint.name} ({i + 1}/{allWaypoints.Length})",
                (float)i / allWaypoints.Length);

            totalConnections += ConnectWaypoint(waypoint, maxDistance, useLineOfSight);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"Connected all waypoints. Total connections created: {totalConnections}");
        EditorUtility.DisplayDialog("Complete",
            $"Successfully connected all waypoints!\nTotal connections: {totalConnections}",
            "OK");
    }

    private void ClearAllConnections()
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();
        int totalCleared = 0;

        foreach (Waypoint waypoint in allWaypoints)
        {
            Undo.RecordObject(waypoint, "Clear All Connections");
            totalCleared += waypoint.neighbors.Count;
            waypoint.neighbors.Clear();
            EditorUtility.SetDirty(waypoint);
        }

        Debug.Log($"Cleared all waypoint connections. Total removed: {totalCleared}");
        EditorUtility.DisplayDialog("Complete",
            $"Cleared all waypoint connections!\nTotal removed: {totalCleared}",
            "OK");
    }
}

// Menu item for scene-wide operations
public class WaypointTools : EditorWindow
{
    private float maxDistance = 10f;
    private bool useLineOfSight = true;

    [MenuItem("Tools/Waypoint Connection Tool")]
    public static void ShowWindow()
    {
        GetWindow<WaypointTools>("Waypoint Tools");
    }

    private void OnGUI()
    {
        GUILayout.Label("Waypoint Connection Settings", EditorStyles.boldLabel);

        maxDistance = EditorGUILayout.FloatField("Max Distance", maxDistance);
        useLineOfSight = EditorGUILayout.Toggle("Use Line of Sight", useLineOfSight);

        EditorGUILayout.Space();

        if (useLineOfSight)
        {
            EditorGUILayout.HelpBox("Line of Sight: Only connects waypoints with clear view (no obstacles).", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Distance Only: Connects all waypoints within max distance.", MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Connect ALL Waypoints in Scene", GUILayout.Height(40)))
        {
            ConnectAllWaypoints();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Connect Selected Waypoints Only", GUILayout.Height(40)))
        {
            ConnectSelectedWaypoints();
        }

        EditorGUILayout.Space();

        GUI.color = Color.red;
        if (GUILayout.Button("Clear ALL Connections in Scene", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Clear All Connections",
                "This will remove all connections between waypoints. Continue?",
                "Yes", "Cancel"))
            {
                ClearAllConnections();
            }
        }
        GUI.color = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Type Change", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Select multiple waypoints in the scene, then use these buttons to change their type.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Selected to General", GUILayout.Height(30)))
        {
            SetSelectedWaypointsType(Waypoint.WaypointType.General);
        }
        if (GUILayout.Button("Set Selected to Roaming", GUILayout.Height(30)))
        {
            SetSelectedWaypointsType(Waypoint.WaypointType.Roaming);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ConnectAllWaypoints()
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();
        int totalConnections = 0;

        EditorUtility.DisplayProgressBar("Connecting Waypoints", "Processing...", 0f);

        for (int i = 0; i < allWaypoints.Length; i++)
        {
            Waypoint waypoint = allWaypoints[i];
            EditorUtility.DisplayProgressBar("Connecting Waypoints",
                $"Processing {waypoint.name} ({i + 1}/{allWaypoints.Length})",
                (float)i / allWaypoints.Length);

            totalConnections += ConnectWaypoint(waypoint, allWaypoints);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"Connected all waypoints. Total connections: {totalConnections}");
        EditorUtility.DisplayDialog("Complete",
            $"Connected all waypoints!\nTotal connections: {totalConnections}",
            "OK");
    }

    private void ConnectSelectedWaypoints()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select waypoints in the scene first.",
                "OK");
            return;
        }

        List<Waypoint> selectedWaypoints = new List<Waypoint>();
        foreach (GameObject obj in Selection.gameObjects)
        {
            Waypoint wp = obj.GetComponent<Waypoint>();
            if (wp != null)
                selectedWaypoints.Add(wp);
        }

        if (selectedWaypoints.Count == 0)
        {
            EditorUtility.DisplayDialog("No Waypoints",
                "No waypoints found in selection.",
                "OK");
            return;
        }

        int totalConnections = 0;
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();

        foreach (Waypoint waypoint in selectedWaypoints)
        {
            totalConnections += ConnectWaypoint(waypoint, allWaypoints);
        }

        Debug.Log($"Connected {selectedWaypoints.Count} selected waypoints. Total connections: {totalConnections}");
        EditorUtility.DisplayDialog("Complete",
            $"Connected {selectedWaypoints.Count} waypoints!\nTotal connections: {totalConnections}",
            "OK");
    }

    private int ConnectWaypoint(Waypoint waypoint, Waypoint[] allWaypoints)
    {
        int connectionsAdded = 0;

        string blockingLayerName = "Barrier"; // Change this to your layer's exact name
        int blockingLayerIndex = LayerMask.NameToLayer(blockingLayerName);

        foreach (Waypoint other in allWaypoints)
        {
            if (other == waypoint) continue;
            if (waypoint.neighbors.Contains(other)) continue;

            float distance = Vector3.Distance(waypoint.transform.position, other.transform.position);
            if (distance > maxDistance) continue;

            if (useLineOfSight)
            {
                Vector3 direction = other.transform.position - waypoint.transform.position;
                Ray ray = new Ray(waypoint.transform.position + direction.normalized * 0.1f, direction);

                if (Physics.Raycast(ray, out RaycastHit hit, distance))
                {
                    // 1. If we hit the target waypoint, the path is clear.
                    if (hit.collider.gameObject == other.gameObject)
                    {
                        // Allow connection
                    }
                    // 2. Check if the object we hit is on the "Bad Layer"
                    else if (hit.collider.gameObject.layer == blockingLayerIndex)
                    {
                        // It hit a layer that blocks connections (like a Wall)
                        continue;
                    }
                    // 3. If it hit something else NOT on that layer (like a trigger or small prop)
                    // we can choose to ignore the hit and connect anyway.
                }
            }

            Undo.RecordObject(waypoint, "Connect Waypoints");
            Undo.RecordObject(other, "Connect Waypoints");

            waypoint.neighbors.Add(other);
            if (!other.neighbors.Contains(waypoint))
            {
                other.neighbors.Add(waypoint);
            }

            EditorUtility.SetDirty(waypoint);
            EditorUtility.SetDirty(other);
            connectionsAdded++;
        }

        return connectionsAdded;
    }

    private void ClearAllConnections()
    {
        Waypoint[] allWaypoints = FindObjectsOfType<Waypoint>();
        int totalCleared = 0;

        foreach (Waypoint waypoint in allWaypoints)
        {
            Undo.RecordObject(waypoint, "Clear All Connections");
            totalCleared += waypoint.neighbors.Count;
            waypoint.neighbors.Clear();
            EditorUtility.SetDirty(waypoint);
        }

        Debug.Log($"Cleared all connections. Total removed: {totalCleared}");
    }

    private void SetSelectedWaypointsType(Waypoint.WaypointType newType)
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select waypoints in the scene first.",
                "OK");
            return;
        }

        List<Waypoint> selectedWaypoints = new List<Waypoint>();
        foreach (GameObject obj in Selection.gameObjects)
        {
            Waypoint wp = obj.GetComponent<Waypoint>();
            if (wp != null)
                selectedWaypoints.Add(wp);
        }

        if (selectedWaypoints.Count == 0)
        {
            EditorUtility.DisplayDialog("No Waypoints",
                "No waypoints found in selection.",
                "OK");
            return;
        }

        foreach (Waypoint waypoint in selectedWaypoints)
        {
            Undo.RecordObject(waypoint, "Change Waypoint Type");
            waypoint.type = newType;
            EditorUtility.SetDirty(waypoint);
        }

        string typeName = newType == Waypoint.WaypointType.General ? "General" : "Roaming";
        Debug.Log($"Changed {selectedWaypoints.Count} waypoints to {typeName} type.");
        EditorUtility.DisplayDialog("Complete",
            $"Changed {selectedWaypoints.Count} waypoints to {typeName} type.",
            "OK");
    }
}