using UnityEngine;
using UnityEditor;
using System.Collections.Generic;




public class PrefabPlacementTool : EditorWindow
{

    public enum Type
    {
        Physical,
        Place,
        Decal,
    }

    public Type type = Type.Physical;
    private Type previousType = Type.Physical; // ������� ���������� ��� �������� ����������� ������

    public enum Mode
    {
        Sequential,
        Random,
    }

    public Mode mode = Mode.Sequential;
   
    [HideInInspector]
    private List<GameObject> prefabsToPlace = new List<GameObject>();
    private List<GameObject> prefabsToRemove = new List<GameObject>();

    public Vector3 rotation;
    public bool randomRotation;
    public Vector3 randomSizeMin = new Vector3(1f, 1f, 1f);
    public Vector3 randomSizeMax = new Vector3(1f, 1f, 1f);
    public bool sizeProportionally = true;

    public float placementHeight = 1.0f;
    private int currentPrefabIndex = 0;
    private int groupIndex = 1;

    [HideInInspector]
    private string savePath = "Assets/PlacmentTool/Prefabs/SavePrefab/"; // ��������� �������� ���� ����������



    public GameObject pointerPrefab;
    public GameObject pointerPrefabHeight;


    private GameObject pointerInstance;
    private GameObject pointerHeightInstance;

    public bool showPrefabsList = true;
    private bool isPlaying = false;





    void FindPointerPrefabs()
    {
        pointerPrefab = FindPrefabByName("PointerPrefab");
        pointerPrefabHeight = FindPrefabByName("PointerHeightPrefab");
    }

    GameObject FindPrefabByName(string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets(prefabName); // ���� ������� � ��������� ������

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]); // ����� ������ ��������� ������
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        else
        {
            Debug.LogError("Prefab with name " + prefabName + " not found!");
            return null;
        }
    }


    private void Update()
    {
        if (isPlaying)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    DeleteObjectUnderCursor();
                }
                else
                {
                    PlaceNextPrefab();
                }
            }
        }


        // �������� �� ��������� ������
        if (type != previousType)
        {

                if (pointerHeightInstance != null)
                {
                    Destroy(pointerHeightInstance);
                    pointerHeightInstance = null;
                }
            

            previousType = type; // ��������� ���������� �����
        }

        MovePointerWithCursor();
    }

    void OnGUI()
    {

        EditorGUILayout.LabelField("Prefab Placement Tool Settings", EditorStyles.boldLabel);

        type = (Type)EditorGUILayout.EnumPopup("Placement Type", type);
        mode = (Mode)EditorGUILayout.EnumPopup("Placement Mode", mode);

        EditorGUILayout.Space();
        List<GameObject> prefabsToRemove = new List<GameObject>();


        for (int i = 0; i < prefabsToPlace.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabsToPlace[i] = (GameObject)EditorGUILayout.ObjectField("Prefab " + i, prefabsToPlace[i], typeof(GameObject), false);
            if (GUILayout.Button("Remove"))
            {
                prefabsToRemove.Add(prefabsToPlace[i]);
            }
            EditorGUILayout.EndHorizontal();
        }

        // �������� ��������� ����� ���������� ��������� ����������
        foreach (var prefab in prefabsToRemove)
        {
            prefabsToPlace.Remove(prefab);
        }
        prefabsToRemove.Clear();

        if (GUILayout.Button("Add Prefab"))
        {
            prefabsToPlace.Add(null);
        }
        GUILayout.Space(20);
        // ��������� ������ "Clear List" ���� ������ ��������
        if (GUILayout.Button("Clear List"))
        {
            prefabsToPlace.Clear();
        }
        EditorGUILayout.Space();
        rotation = EditorGUILayout.Vector3Field("Rotation", rotation);
        randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);

        EditorGUILayout.Space();
        sizeProportionally = EditorGUILayout.Toggle("Size Proportionally", sizeProportionally);

        if (sizeProportionally)
        {
            float minSize = EditorGUILayout.FloatField("Random Size Min", randomSizeMin.x);
            randomSizeMin = new Vector3(minSize, minSize, minSize);

            float maxSize = EditorGUILayout.FloatField("Random Size Max", randomSizeMax.x);
            randomSizeMax = new Vector3(maxSize, maxSize, maxSize);
        }
        else
        {
            randomSizeMin = EditorGUILayout.Vector3Field("Random Size Min", randomSizeMin);
            randomSizeMax = EditorGUILayout.Vector3Field("Random Size Max", randomSizeMax);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Objects"))
        {
            ClearObjects();
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Group Objects"))
        {
            GroupObjects();
        }

        if (GUILayout.Button("Save Grouped Prefab"))
        {
            SavePrefabs();
        }

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.Space();

        GUILayout.Label("Save Path", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField(savePath);
        if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Save Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // ���� ������� �����, ��������� savePath
                savePath = "Assets" + selectedPath.Replace(Application.dataPath, "") + "/";
            }
        }
        GUILayout.EndHorizontal();

        placementHeight = EditorGUILayout.FloatField("Placement Height", placementHeight);
      
        pointerPrefab = (GameObject)EditorGUILayout.ObjectField("Pointer Prefab", pointerPrefab, typeof(GameObject), false);
        pointerPrefabHeight = (GameObject)EditorGUILayout.ObjectField("Pointer Prefab Height", pointerPrefabHeight, typeof(GameObject), false);



        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            GUI.enabled = false; // ���������� ������ � ������ ��������������
        }

        if (GUILayout.Button(isPlaying ? "Stop" : "Play"))
        {
            TogglePlay();
        }

        GUI.enabled = true; // �������������� ���������� ��� ������ ��������� ����������


    }

    private void OnEnable()
    {
        FindPointerPrefabs(); // �������� ����� ������ �������� ��� ��������� ���� ����������
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && isPlaying)
        {
            isPlaying = false;
            HidePointer();
        }
    }

    private void TogglePlay()
    {
        if (EditorApplication.isPlaying)
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                Debug.Log("Prefab Placement Tool Playing");
                CreatePointer();
            }
            else
            {
                Debug.Log("Prefab Placement Tool Stopped");
                HidePointer();
            }
        }
        else
        {
            Debug.LogWarning("You can only toggle play mode while in play mode.");
        }
    }



    [MenuItem("Window/Prefab Placement Tool")]
    public static void ToggleWindow()
    { 
        GetWindow<PrefabPlacementTool>("Prefab Placement Tool");
    }

    void DeleteObjectUnderCursor()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            GameObject objectToDelete = hit.collider.gameObject;

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                if (objectToDelete.CompareTag("PlacedObject"))
                {
                    Undo.DestroyObjectImmediate(objectToDelete);
                }
            }
        }
    }
    Quaternion GenerateRandomRotation(Vector3 maxRotation)
    {
        if (randomRotation)
        {
            return Quaternion.Euler(
                Random.Range(0f, maxRotation.x),
                Random.Range(0f, maxRotation.y),
                Random.Range(0f, maxRotation.z)
            );
        }
        else
        {
            return Quaternion.Euler(maxRotation);
        }
    }
    void PlaceNextPrefab()
    {
        if (prefabsToPlace.Count == 0)
        {
            Debug.LogWarning("List of prefabs to place is empty.");
            return;
        }

        GameObject prefabToInstantiate = null;

        if (mode == Mode.Sequential)
        {
            if (currentPrefabIndex >= prefabsToPlace.Count)
            {
                currentPrefabIndex = 0; // Return to the beginning of the list
            }

            prefabToInstantiate = prefabsToPlace[currentPrefabIndex];
            currentPrefabIndex++;
        }
        else if (mode == Mode.Random)
        {
            int randomIndex = Random.Range(0, prefabsToPlace.Count);
            prefabToInstantiate = prefabsToPlace[randomIndex];
        }

        if (prefabToInstantiate != null)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 spawnPosition = hit.point;
                Quaternion spawnRotation = GenerateRandomRotation(rotation);

                Vector3 GenerateRandomScale(Vector3 min, Vector3 max)
                {
                    if (sizeProportionally)
                    {
                        float randomSize = Random.Range(min.x, max.x);
                        return new Vector3(randomSize, randomSize, randomSize);
                    }
                    else
                    {
                        return new Vector3(
                            Random.Range(min.x, max.x),
                            Random.Range(min.y, max.y),
                            Random.Range(min.z, max.z)
                        );
                    }
                }

                if (type == Type.Place || type == Type.Decal || type == Type.Physical)
                {
                    Vector3 randomScale = GenerateRandomScale(randomSizeMin, randomSizeMax);

                    if (type == Type.Place)
                    {
                        spawnPosition.y += 0.1f;
                        GameObject newObject = Instantiate(prefabToInstantiate, spawnPosition, spawnRotation);
                        newObject.tag = "PlacedObject";
                        newObject.transform.localScale = randomScale;
                     
                        Rigidbody createdRB = newObject.GetComponent<Rigidbody>();
                        if (createdRB == null)
                        {
                            createdRB = newObject.AddComponent<Rigidbody>();
                            createdRB.mass = 1f; // Set high mass
                            createdRB.linearDamping = 20f; // Set movement resistance
                            createdRB.angularDamping = 10f; // Set rotation resistance

                  
                        }

                    }
                    else if (type == Type.Decal)
                    {
                        Vector3 decalSpawnPosition = hit.point + hit.normal * 0.001f;
                        Quaternion decalSpawnRotation = spawnRotation;
                        GameObject newDecalObject = Instantiate(prefabToInstantiate, decalSpawnPosition, decalSpawnRotation);
                        newDecalObject.tag = "PlacedObject";
                        newDecalObject.transform.localScale = randomScale;


        
                    }
                    else if (type == Type.Physical)
                    {
                        spawnPosition.y += placementHeight;
                        GameObject newPhysicalObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstantiate);
                        newPhysicalObject.transform.position = spawnPosition;
                        newPhysicalObject.transform.rotation = spawnRotation;
                        newPhysicalObject.tag = "PlacedObject";
                        newPhysicalObject.transform.localScale = randomScale;


                        Rigidbody createdPhysicalRB = newPhysicalObject.GetComponent<Rigidbody>();
                        if (createdPhysicalRB == null)
                        {
                            createdPhysicalRB = newPhysicalObject.AddComponent<Rigidbody>();
                        }

                        Vector3 randomPhysicalScale = GenerateRandomScale(randomSizeMin, randomSizeMax);
                        newPhysicalObject.transform.localScale = randomPhysicalScale;

                        createdPhysicalRB.mass = 40 * (newPhysicalObject.transform.localScale.x * newPhysicalObject.transform.localScale.y * newPhysicalObject.transform.localScale.z);
                    }

                }
            }
        }
    }






    void MovePointerWithCursor()
    {
        if (isPlaying)
        {
            if (pointerInstance == null && pointerPrefab != null)
            {
                // Instantiate the pointerPrefab if it hasn't been instantiated yet
                pointerInstance = Instantiate(pointerPrefab);
            }

            // Check if the pointerInstance exists and the mouse position is valid
            if (pointerInstance != null && Camera.main != null)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    // Update the position of the pointer prefab to match the cursor's position
                    pointerInstance.transform.position = hit.point;
                }
            }

            if (type == Type.Physical)
            {
                if (pointerPrefabHeight != null)
                {
                    if (pointerHeightInstance == null)
                    {
                        // Instantiate the pointerPrefabHeight if it hasn't been instantiated yet
                        pointerHeightInstance = Instantiate(pointerPrefabHeight);
                    }

                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        // Set the position of the pointerPrefabHeight to the height above the hit point
                        Vector3 heightPosition = hit.point + Vector3.up * placementHeight;
                        pointerHeightInstance.transform.position = heightPosition;
                    }
                }

            }
        }
    }

    public void GroupObjects()
    {
        // ������� ������ GameObject, ������� ����� ������� �������� ��� ����������� ��������
        GameObject groupRoot = new GameObject("GroupedObjects_" + Random.Range(1000, 9999)); // ���������� ��������� �������� � ��������� �� 1000 �� 9999

        // ��������� ������ GroupedObjectMarker � ��������� �������, ����� ��� ����� ���� ����� ��������
        GroupedObjectMarker marker = groupRoot.AddComponent<GroupedObjectMarker>();

        // ������� ��� ������� � ����� "PlacedObject" � ������ �� ��������� ��������� ��������� �������
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("PlacedObject");
        foreach (GameObject obj in allObjects)
        {
            // ���������, ��� ������ �� �������� �������� �������� ������ ������
            if (obj.transform.parent == null || obj.transform.parent.GetComponent<GroupedObjectMarker>() == null)
            {
                obj.transform.parent = groupRoot.transform;
            }
        }
    }



    public void SavePrefabs()
    {
        // Find all objects with the GroupedObjectMarker script attached
        GroupedObjectMarker[] groupedObjects = FindObjectsOfType<GroupedObjectMarker>();

        foreach (GroupedObjectMarker marker in groupedObjects)
        {
            // Get the root object
            Transform root = marker.transform;

            // Get the original name of the root GameObject
            string originalName = root.gameObject.name;

            // Create a list to hold references to the placed objects within this group
            List<GameObject> objectsToSave = new List<GameObject>();

            // Find all children of the current group root
            foreach (Transform child in root)
            {
                objectsToSave.Add(child.gameObject);
            }

            // Modify objects before saving as prefabs
            foreach (GameObject obj in objectsToSave)
            {
                // Enable colliders
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                foreach (Collider _coll in colliders)
                {
                    _coll.enabled = true;
                }

                // Remove MeshCollider components
                MeshCollider[] meshColliders = obj.GetComponentsInChildren<MeshCollider>();
                foreach (MeshCollider meshColl in meshColliders)
                {
                    DestroyImmediate(meshColl);
                }

                // Remove Rigidbody components
                Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    DestroyImmediate(rb);
                }
            }

            // Remove the GroupedObjectMarker component before saving as a prefab
            DestroyImmediate(marker);

            // Create a prefab containing the modified objects within this group with the original name
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, savePath + originalName + ".prefab");


            if (prefab == null)
            {
                Debug.Log("Folder not Found");
            }
            else
            {
                Debug.Log("Prefab saved at: " + savePath + originalName + ".prefab");
                // Restore the GroupedObjectMarker component after saving the prefab (optional)
                root.gameObject.AddComponent<GroupedObjectMarker>();
            }
        }
    }


    void CreatePointer()
    {
        if (pointerPrefab != null)
        {
            pointerInstance = Instantiate(pointerPrefab);

            if (type == Type.Physical && pointerPrefabHeight != null)
            {
                pointerHeightInstance = Instantiate(pointerPrefabHeight);
            }
        }
    }

    void HidePointer()
    {
        if (pointerInstance != null)
        {
            Destroy(pointerInstance);
            pointerInstance = null;
        }
        if (pointerHeightInstance != null)
        {
            Destroy(pointerHeightInstance);
            pointerHeightInstance = null;
        }
    }
    public class GroupedObjectMarker : MonoBehaviour
    {
        // ������ ������ ��� ����������� �������������� ��������
    }

    public void ClearObjects()
    {
        GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("PlacedObject");
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }
    }



}