// Controller.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    public GameObject[] spawnablePrefabs;

    private int selectedPrefabIndex = 0;
    private Camera mainCamera;
    
    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandlePrefabSelection();
        HandleCameraControls();
        HandleSpawning();
    }

    void HandlePrefabSelection()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedPrefabIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedPrefabIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) selectedPrefabIndex = 2;
    }

    void HandleCameraControls()
    {
        float cameraSpeed = 0.1f;
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(cameraSpeed, 0, 0);
        }
        else if (Keyboard.current.leftArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(-cameraSpeed, 0, 0);
        }
        
        if (Keyboard.current.upArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(0, cameraSpeed, 0);
        }
        else if (Keyboard.current.downArrowKey.isPressed)
        {
            mainCamera.transform.position += new Vector3(0, -cameraSpeed, 0);
        }
    }
    
    void HandleSpawning()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnSelectedPrefab();
        }
    }
    
    void SpawnSelectedPrefab()
    {
        if (selectedPrefabIndex >= spawnablePrefabs.Length || spawnablePrefabs[selectedPrefabIndex] == null)
        {
            Debug.LogWarning("Selected prefab is not valid. Cannot spawn.");
            return;
        }

        Vector3 spawnPosition = mainCamera.ScreenToWorldPoint(Pointer.current.position.ReadValue());
        spawnPosition.z = 0;

        Instantiate(spawnablePrefabs[selectedPrefabIndex], spawnPosition, Quaternion.identity);
    }
}