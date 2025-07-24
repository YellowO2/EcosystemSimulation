using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Controller : MonoBehaviour
{
    [Header("References")]
    public WorldGenerator worldGenerator;
    private Camera mainCamera;

    [Header("Tile Palette")]
    public Tile[] placeableTiles;
    private int selectedTileIndex = 0;

    private Vector3Int lastModifiedCell;

    void Awake()
    {
        mainCamera = Camera.main;
        lastModifiedCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
    }

    void Update()
    {
        HandleTileSelection();
        HandleCameraControls();
        HandleTileModification();
    }

    void HandleTileSelection()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame && placeableTiles.Length > 0) selectedTileIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && placeableTiles.Length > 1) selectedTileIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && placeableTiles.Length > 2) selectedTileIndex = 2;
    }

    void HandleCameraControls()
    {
        float cameraSpeed = 10f * Time.deltaTime;
        if (Keyboard.current.rightArrowKey.isPressed) mainCamera.transform.position += new Vector3(cameraSpeed, 0, 0);
        if (Keyboard.current.leftArrowKey.isPressed) mainCamera.transform.position += new Vector3(-cameraSpeed, 0, 0);
        if (Keyboard.current.upArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, cameraSpeed, 0);
        if (Keyboard.current.downArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, -cameraSpeed, 0);
    }

    void HandleTileModification()
    {
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int currentCell = worldGenerator.groundTilemap.WorldToCell(worldPoint);

        if (Mouse.current.leftButton.isPressed)
        {
            if (currentCell != lastModifiedCell && selectedTileIndex < placeableTiles.Length)
            {
                Tile selectedTile = placeableTiles[selectedTileIndex];
                worldGenerator.SetTile(currentCell, selectedTile);
                lastModifiedCell = currentCell;
            }
        }
        else if (Mouse.current.rightButton.isPressed)
        {
            if (currentCell != lastModifiedCell)
            {
                worldGenerator.SetTile(currentCell, null);
                lastModifiedCell = currentCell;
            }
        }
        else
        {
            lastModifiedCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }
    }
}