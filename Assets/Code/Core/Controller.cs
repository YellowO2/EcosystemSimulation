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
    public CreatureUIManager uiManager;

    [Header("UI (UXML)")]
    public UIDocument mainUIDocument;

    [Header("Construction")]
    public List<PlaceableItem> hotbarItems;
    private PlaceableItem currentSelectedItem;
    private VisualElement hotbarSlotsContainer;
    private VisualElement currentlySelectedSlotElement;

    [Header("Camera Controls")]
    private float zoomSpeed = 20f;
    private float minZoom = 2f;
    private float maxZoom = 50f;
    private Camera mainCamera;
    private string currentWorldName;
    private Vector3Int lastModifiedCell;

    // UXML Element References
    private VisualElement root;
    private VisualElement gymSetupPanel;
    private VisualElement setupContent;
    private Button startGymButton;
    private Button toggleSetupButton;
    private VisualElement speciesListContainer;
    private Slider timeScaleSlider;
    private Slider mutationSlider;
    private Slider durationSlider;
    private Slider populationSlider;
    private Button saveWorldButton;
    private Label simStatsLabel;
    private bool setupPanelCollapsed = false;
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
        setupContent = root.Q<VisualElement>("setup-content");
        startGymButton = root.Q<Button>("start-button");
        toggleSetupButton = root.Q<Button>("toggle-setup-button");
        speciesListContainer = root.Q<VisualElement>("species-list");
        startGymButton.clicked += StartGymSimulation;
        toggleSetupButton.clicked += ToggleSetupPanel;

        timeScaleSlider = root.Q<Slider>("time-scale-slider");
        mutationSlider = root.Q<Slider>("mutation-slider");
        durationSlider = root.Q<Slider>("duration-slider");
        populationSlider = root.Q<Slider>("population-slider");
        simStatsLabel = root.Q<Label>("sim-stats-label");
        saveWorldButton = root.Q<Button>("save-world-button");
        timeScaleSlider.RegisterValueChangedCallback(evt => HandleTimeScaleChanged(evt.newValue));
        mutationSlider.RegisterValueChangedCallback(evt => HandleMutationChanged(evt.newValue));
        durationSlider.RegisterValueChangedCallback(evt => HandleDurationChanged(evt.newValue));
        populationSlider.RegisterValueChangedCallback(evt => HandlePopulationChanged(evt.newValue));
        saveWorldButton.clicked += HandleSaveCurrentWorld;

        hotbarSlotsContainer = root.Q<VisualElement>("hotbar-slots-container");
        root.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        if (startGymButton != null) startGymButton.clicked -= StartGymSimulation;
        if (toggleSetupButton != null) toggleSetupButton.clicked -= ToggleSetupPanel;
        if (saveWorldButton != null) saveWorldButton.clicked -= HandleSaveCurrentWorld;
    }

    void Start()
    {
        PopulateSpeciesToggles();
        timeScaleSlider.value = Time.timeScale;
        mutationSlider.value = populationManager.globalMutationMultiplier;
        durationSlider.value = populationManager.generationTime;
        populationSlider.value = populationManager.populationOverride > 0 ? populationManager.populationOverride : 50;
        UpdateSimStatsText();

        PopulateHotbar();
        if (hotbarItems.Count > 0)
        {
            SelectItem(hotbarItems[0], hotbarSlotsContainer.ElementAt(0));
        }
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            worldMenuManager.ShowMenu(currentWorldName);
        }

        if (worldMenuManager.IsVisible) return;

        HandleCameraControls();
        HandleConstruction();
        UpdateSimStatsText();
    }
    #endregion


    #region Public Methods
    public void ShowHUD()
    {
        root.style.display = DisplayStyle.Flex;
        gymSetupPanel.style.display = DisplayStyle.Flex;
        
        // Collapse setup panel content by default if creatures already exist
        if (populationManager.HasExistingCreatures())
        {
            setupPanelCollapsed = true;
            setupContent.style.display = DisplayStyle.None;
            toggleSetupButton.text = "☰"; // Hamburger icon
        }
        else
        {
            setupPanelCollapsed = false;
            setupContent.style.display = DisplayStyle.Flex;
            toggleSetupButton.text = "─"; // Minimize icon
        }
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
    private void ToggleSetupPanel()
    {
        setupPanelCollapsed = !setupPanelCollapsed;
        
        if (setupPanelCollapsed)
        {
            setupContent.style.display = DisplayStyle.None;
            toggleSetupButton.text = "☰"; // Hamburger icon
        }
        else
        {
            setupContent.style.display = DisplayStyle.Flex;
            toggleSetupButton.text = "─"; // Minimize icon
        }
    }

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
            populationManager.SpawnInitialCreatures(selectedSpeciesNames);
            // Collapse the setup content instead of hiding the whole panel
            setupPanelCollapsed = true;
            setupContent.style.display = DisplayStyle.None;
            toggleSetupButton.text = "☰"; // Hamburger icon
        }
    }
    #endregion


    #region In-Game Controls
    private void HandleCameraControls()
    {
        // Panning Controls  ---
        float cameraSpeed = 10f * Time.unscaledDeltaTime;
        if (Keyboard.current.rightArrowKey.isPressed) mainCamera.transform.position += new Vector3(cameraSpeed, 0, 0);
        if (Keyboard.current.leftArrowKey.isPressed) mainCamera.transform.position += new Vector3(-cameraSpeed, 0, 0);
        if (Keyboard.current.upArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, cameraSpeed, 0);
        if (Keyboard.current.downArrowKey.isPressed) mainCamera.transform.position += new Vector3(0, -cameraSpeed, 0);

        // --- Zoom Controls ---
        float scroll = Mouse.current.scroll.y.ReadValue();

        if (scroll != 0)
        {

            // get world position before zooming
            Vector3 mousePosBeforeZoom = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Calculate the new orthographic size
            float newSize = mainCamera.orthographicSize - scroll * zoomSpeed * Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);

            // Get the world position under the mouse after zooming
            Vector3 mousePosAfterZoom = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Calculate the difference and move the camera to counteract the shift
            Vector3 offset = mousePosBeforeZoom - mousePosAfterZoom;
            mainCamera.transform.position += offset;
        }
    }

    private void HandleConstruction()
    {
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int currentCell = worldManager.groundTilemap.WorldToCell(worldPoint);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log($"Mouse clicked at cell: {currentCell}");
            // Todo: this is actually more lik handle mouse click then handle construction. Might change later
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

            Debug.Log($"hit.collider: {hitCollider?.name}");
            if (hitCollider != null)
            {
                Creature clickedCreature = hitCollider.GetComponent<Creature>();
                Debug.Log($"Clicked on: {clickedCreature?.name}");
                if (clickedCreature != null)
                {
                    // --- THIS IS THE KEY ---
                    // We found a creature. Tell the UI Manager to select it.
                    uiManager.SelectCreature(clickedCreature);
                    return; // Stop here. Don't try to build anything.
                }
            }

            // If we've reached this point, we didn't click a creature or UI.
            // Deselect any currently selected creature.
            uiManager.Deselect();
            if (currentSelectedItem != null)
            {
                currentSelectedItem.Place(this, worldManager, currentCell);
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
    private void PopulateHotbar()
    {
        hotbarSlotsContainer.Clear();
        foreach (var item in hotbarItems)
        {
            var slot = new VisualElement();
            slot.AddToClassList("hotbar-slot");
            slot.style.backgroundImage = new StyleBackground(item.icon);
            slot.RegisterCallback<ClickEvent>(evt => SelectItem(item, slot));
            hotbarSlotsContainer.Add(slot);
        }
    }

    private void SelectItem(PlaceableItem item, VisualElement slotElement)
    {
        if (currentlySelectedSlotElement != null)
        {
            currentlySelectedSlotElement.RemoveFromClassList("hotbar-slot--selected");
        }
        currentSelectedItem = item;
        currentlySelectedSlotElement = slotElement;
        currentlySelectedSlotElement.AddToClassList("hotbar-slot--selected");
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

    private void HandleDurationChanged(float value)
    {
        populationManager.generationTime = value;
        UpdateSimStatsText();
    }

    private void HandlePopulationChanged(float value)
    {
        populationManager.populationOverride = (int)value;
        UpdateSimStatsText();
    }

    private void UpdateSimStatsText()
    {
        float speed = timeScaleSlider.value;
        float mutation = mutationSlider.value;
        float duration = durationSlider.value;
        int pop = (int)populationSlider.value;
        int generation = populationManager.currentGeneration;
        simStatsLabel.text = $"Gen: {generation} | Pop: {pop} | Speed: {speed:F1}x | Mutation: {mutation:F1}x | Duration: {duration:F0}s";
    }
    #endregion
}