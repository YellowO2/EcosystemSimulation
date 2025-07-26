using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class Controller : MonoBehaviour
{
    #region Fields & References
    [Header("System References")]
    public WorldGenerator worldManager;
    public PopulationManager populationManager;
    public SpeciesDatabase speciesDatabase;
    public WorldMenuManager worldMenuManager;

    [Header("UI (UXML)")]
    public UIDocument mainUIDocument;

    [Header("Tile Editing")]
    public Tile[] placeableTiles;

    private Camera mainCamera;
    private string currentWorldName;
    private int selectedTileIndex = 0;
    private Vector3Int lastModifiedCell;

    // UXML Element References
    private VisualElement root;
    private VisualElement gymSetupPanel;
    private Button startGymButton;
    private VisualElement speciesListContainer;
    private Slider timeScaleSlider;
    private Slider mutationSlider;
    private Button saveWorldButton;
    private Label simStatsLabel;
    #endregion


    #region Unity Lifecycle & UI Setup
    void Awake()
    {
        mainCamera = Camera.main;
        lastModifiedCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
    }

    void OnEnable()
    {
        root = mainUIDocument.rootVisualElement;

        gymSetupPanel = root.Q<VisualElement>("gym-setup-panel");
        startGymButton = root.Q<Button>("start-button");
        speciesListContainer = root.Q<VisualElement>("species-list");
        startGymButton.clicked += StartGymSimulation;

        timeScaleSlider = root.Q<Slider>("time-scale-slider");
        mutationSlider = root.Q<Slider>("mutation-slider");
        simStatsLabel = root.Q<Label>("sim-stats-label");
        saveWorldButton = root.Q<Button>("save-world-button");
        timeScaleSlider.RegisterValueChangedCallback(evt => HandleTimeScaleChanged(evt.newValue));
        mutationSlider.RegisterValueChangedCallback(evt => HandleMutationChanged(evt.newValue));
        saveWorldButton.clicked += HandleSaveCurrentWorld;

        root.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        if (startGymButton != null) startGymButton.clicked -= StartGymSimulation;
        if (saveWorldButton != null) saveWorldButton.clicked -= HandleSaveCurrentWorld;
    }

    void Start()
    {
        PopulateSpeciesToggles();
        timeScaleSlider.value = Time.timeScale;
        mutationSlider.value = populationManager.globalMutationMultiplier;
        UpdateSimStatsText();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            worldMenuManager.ShowMenu(currentWorldName);
        }

        if (worldMenuManager.IsVisible) return;

        HandleCameraControls();
        HandleTileSelection();
        HandleTileModification();
    }
    #endregion


    #region Public Methods
    public void ShowHUD()
    {
        root.style.display = DisplayStyle.Flex;
        gymSetupPanel.style.display = DisplayStyle.Flex; // Ensure gym panel is visible when HUD shows
    }

    public void HideHUD()
    {
        root.style.display = DisplayStyle.None;
    }

    public void SetCurrentWorld(string worldName)
    {
        this.currentWorldName = worldName;
    }

    public void ResumeTime()
    {
        HandleTimeScaleChanged(timeScaleSlider.value);
    }
    #endregion


    #region Gym UI (UXML)
    private void PopulateSpeciesToggles()
    {
        speciesListContainer.Clear();
        foreach (var speciesConfig in speciesDatabase.allSpecies)
        {
            var toggle = new Toggle(speciesConfig.speciesName) { name = speciesConfig.speciesName };
            speciesListContainer.Add(toggle);
        }
    }

    private void StartGymSimulation()
    {
        var selectedSpeciesNames = new List<string>();
        var toggles = speciesListContainer.Query<Toggle>().ToList();
        foreach (var toggle in toggles)
        {
            if (toggle.value) selectedSpeciesNames.Add(toggle.name);
        }

        if (selectedSpeciesNames.Count > 0)
        {
            populationManager.ConfigureAndStartSimulation(selectedSpeciesNames);
            gymSetupPanel.style.display = DisplayStyle.None; // Hide the panel after starting
        }
    }
    #endregion


    #region In-Game Controls
    private void HandleCameraControls()
    {
        float cameraSpeed = 10f * Time.unscaledDeltaTime;
        //debug log for key press
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            Debug.Log("Camera movement key pressed");
        }
        if (Keyboard.current.rightArrowKey.isPressed) mainCamera.transform.position += new Vector3(cameraSpeed, 0, 0);
        if (Keyboard.current.leftArrowKey.isPressed) mainCamera.transform.position += new Vector3(-cameraSpeed, 0, 0);
        if (Keyboard.current.upArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, cameraSpeed, 0);
        if (Keyboard.current.downArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, -cameraSpeed, 0);
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

    private void HandleSaveCurrentWorld()
    {
        if (string.IsNullOrEmpty(currentWorldName))
        {
            Debug.LogWarning("Cannot save world without a name.");
            return;
        }
        DatabaseManager.Instance.SaveWorld(currentWorldName);
        Debug.Log($"World '{currentWorldName}' saved successfully.");
    }
    #endregion



    #region In-Game UI (HUD)
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
        simStatsLabel.text = $"Speed: {speed:F1}x | Mutation: {mutation:F1}x";
    }
    #endregion
}