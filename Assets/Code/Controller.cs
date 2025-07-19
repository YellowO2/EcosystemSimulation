
// WorldSpawner.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour

{

    public GameObject[] spawnablePrefabs;

    private int selectedPrefabIndex = 0;
    private Camera mainCamera;
    private Transform followTarget;

    void Awake()
    {
        mainCamera = Camera.main;
    }


    void Update()
    {
        // --- 1. Select what to spawn ---
        HandlePrefabSelection();
        // --- 2. Handle camera controls ---
        HandleCameraControls();
        // --- 2. Spawn it with a mouse click ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"Spawning prefab: {spawnablePrefabs[selectedPrefabIndex].name}");
            // If the space key is pressed, spawn the selected prefab.
            SpawnSelectedPrefab();
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnSelectedPrefab();
        }
    }

    void HandlePrefabSelection()
    {
        // Use number keys to select an item from the list.
        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedPrefabIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedPrefabIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) selectedPrefabIndex = 2;
    }

    void HandleCameraControls()
    {
        // Right key moves the camera right, left key moves it left.
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(0.1f, 0, 0);
        }
        else if (Keyboard.current.leftArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(-0.1f, 0, 0);
        }
    }


    void SpawnSelectedPrefab()
    {
        // Safety check to make sure the selected index is valid.
        if (selectedPrefabIndex >= spawnablePrefabs.Length || spawnablePrefabs[selectedPrefabIndex] == null)
        {
            Debug.LogWarning("Selected prefab is not valid. Cannot spawn.");
            return;
        }


        Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());
        //debug mouse position

        spawnPosition.z = 0;

        // Get the chosen prefab from our list and instantiate it.
        GameObject prefabToSpawn = spawnablePrefabs[selectedPrefabIndex];
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }
}
