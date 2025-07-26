using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Controller : MonoBehaviour
{
    [Header("Game References")]
    public WorldGenerator worldManager;
    public PopulationManager populationManager;

    [Header("World Selection UI")]
    public GameObject worldSelectionPanel;
    public Transform worldListContainer;
    public GameObject worldButtonPrefab;
    public TMP_InputField newWorldNameInput;
    public Button createWorldButton;
    public Button saveCurrentWorldButton;

    [Header("In-Game UI")]
    public Slider timeScaleSlider;
    public Slider mutationSlider;
    public TextMeshProUGUI simStatsText;

    [Header("Tile Editing")]
    public Tile[] placeableTiles;
    private int selectedTileIndex = 0;
    private Vector3Int lastModifiedCell;
    
    private Camera mainCamera;
    private string currentWorldName;

    void Awake()
    {
        mainCamera = Camera.main;
        lastModifiedCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
    }
    
    void Start()
    {
        createWorldButton.onClick.AddListener(HandleCreateNewWorld);
        saveCurrentWorldButton.onClick.AddListener(HandleSaveCurrentWorld);
        timeScaleSlider.onValueChanged.AddListener(HandleTimeScaleChanged);
        mutationSlider.onValueChanged.AddListener(HandleMutationChanged);

        UpdateSimStatsText();
        ShowWorldSelectionMenu();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowWorldSelectionMenu();
        }

        if (worldSelectionPanel.activeSelf) return;

        HandleCameraControls();
        HandleTileSelection();
        HandleTileModification();
    }

    private void HandleTileSelection()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame && placeableTiles.Length > 0) selectedTileIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && placeableTiles.Length > 1) selectedTileIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && placeableTiles.Length > 2) selectedTileIndex = 2;
    }

    private void HandleTileModification()
    {
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int currentCell = worldManager.groundTilemap.WorldToCell(worldPoint);

        if (Mouse.current.leftButton.isPressed)
        {
            if (currentCell != lastModifiedCell && selectedTileIndex < placeableTiles.Length)
            {
                worldManager.SetTile(currentCell, placeableTiles[selectedTileIndex]);
                lastModifiedCell = currentCell;
            }
        }
        else if (Mouse.current.rightButton.isPressed)
        {
            if (currentCell != lastModifiedCell)
            {
                worldManager.SetTile(currentCell, null);
                lastModifiedCell = currentCell;
            }
        }
        else
        {
            lastModifiedCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }
    }

    private void HandleTimeScaleChanged(float value)
    {
        Time.timeScale = value;
        UpdateSimStatsText();
    }
    
    private void HandleMutationChanged(float value)
    {
        populationManager.globalMutationMultiplier = value;
        UpdateSimStatsText();
    }

    private void UpdateSimStatsText()
    {
        float speed = timeScaleSlider.value;
        float mutation = mutationSlider.value;
        simStatsText.text = $"Speed: {speed:F1}x | Mutation: {mutation:F1}x";
    }

    private void ShowWorldSelectionMenu()
    {
        worldSelectionPanel.SetActive(true);
        Time.timeScale = 0f;
        PopulateWorldList();
        saveCurrentWorldButton.interactable = !string.IsNullOrEmpty(currentWorldName);
    }
    
    private void PopulateWorldList()
    {
        foreach (Transform child in worldListContainer) Destroy(child.gameObject);
        List<string> worldNames = DatabaseManager.Instance.GetSavedWorldNames();
        foreach (string worldName in worldNames)
        {
            GameObject prefabInstance = Instantiate(worldButtonPrefab, worldListContainer);
            Button loadButton = prefabInstance.transform.Find("LoadButton").GetComponent<Button>();
            Button deleteButton = prefabInstance.transform.Find("DeleteButton").GetComponent<Button>();
            loadButton.GetComponentInChildren<TextMeshProUGUI>().text = worldName;
            
            string nameForButton = worldName;
            loadButton.onClick.AddListener(() => HandleLoadWorld(nameForButton));
            deleteButton.onClick.AddListener(() => HandleDeleteWorld(nameForButton));
        }
    }

    private void HandleDeleteWorld(string worldName)
    {
        DatabaseManager.Instance.DeleteWorld(worldName);
        if (worldName == currentWorldName) currentWorldName = null;
        PopulateWorldList();
    }

    private void HandleCreateNewWorld()
    {
        string worldName = newWorldNameInput.text;
        if (string.IsNullOrWhiteSpace(worldName)) return;
        currentWorldName = worldName;
        
        worldManager.GenerateNewWorld(WorldGenerator.WorldType.Perlin);
        populationManager.StartFreshSimulation();
        DatabaseManager.Instance.SaveWorld(currentWorldName);
        
        CloseMenuAndResume();
    }
    
    private void HandleLoadWorld(string worldName)
    {
        currentWorldName = worldName;
        DatabaseManager.Instance.LoadWorld(currentWorldName);
        CloseMenuAndResume();
    }

    private void HandleSaveCurrentWorld()
    {
        if (string.IsNullOrEmpty(currentWorldName)) return;
        DatabaseManager.Instance.SaveWorld(currentWorldName);
        CloseMenuAndResume();
    }
    
    private void CloseMenuAndResume()
    {
        worldSelectionPanel.SetActive(false);
        HandleTimeScaleChanged(timeScaleSlider.value);
    }

    void HandleCameraControls()
    {
        float cameraSpeed = 10f * Time.unscaledDeltaTime;
        if (Keyboard.current.rightArrowKey.isPressed) mainCamera.transform.position += new Vector3(cameraSpeed, 0, 0);
        if (Keyboard.current.leftArrowKey.isPressed) mainCamera.transform.position += new Vector3(-cameraSpeed, 0, 0);
        if (Keyboard.current.upArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, cameraSpeed, 0);
        if (Keyboard.current.downArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, -cameraSpeed, 0);
    }
}