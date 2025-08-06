using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class WorldMenuManager : MonoBehaviour
{
    [Header("System References")]
    public WorldGenerator worldManager;
    public PopulationManager populationManager;
    public Controller gameController;
    public UIDocument menuDocument;
    public WorldDatabase worldDatabase;

    private VisualElement menuRoot;
    private VisualElement worldListContainer;
    private DropdownField presetDropdown; 
    private TextField newWorldNameInput;
    private Button createWorldButton;
    public bool IsVisible => menuRoot != null && menuRoot.style.display == DisplayStyle.Flex;

    private string currentWorldName;

    void Awake()
    {
        menuRoot = menuDocument.rootVisualElement;
        
        worldListContainer = menuRoot.Q<VisualElement>("world-list-container");
        presetDropdown = menuRoot.Q<DropdownField>("preset-dropdown");
        newWorldNameInput = menuRoot.Q<TextField>("new-world-name-input");
        createWorldButton = menuRoot.Q<Button>("create-world-button");
     
        
        createWorldButton.clicked += HandleCreateNewWorld;
        
        menuRoot.style.display = DisplayStyle.None;
    }

    void Start()
    {
        ShowMenu();
    }

    public void ShowMenu(string currentWorld = null)
    {
        currentWorldName = currentWorld;
        menuRoot.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f;
        
        PopulateWorldList();
        PopulatePresetDropdown(); // Populate the new dropdown
    
        gameController.HideHUD();
    }

    private void PopulateWorldList()
    {
        worldListContainer.Clear();
        List<string> worldNames = DatabaseManager.Instance.GetSavedWorldNames();
        foreach (string worldName in worldNames)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }};
            var loadButton = new Button(() => HandleLoadWorld(worldName)) { text = worldName, style = { flexGrow = 1 } };
            var deleteButton = new Button(() => HandleDeleteWorld(worldName)) { text = "X", style = { width = 30, marginLeft = 5 } };
            row.Add(loadButton);
            row.Add(deleteButton);
            worldListContainer.Add(row);
        }
    }

    private void PopulatePresetDropdown()
    {
        if (worldDatabase == null || worldDatabase.allWorldPresets.Count == 0) return;
        presetDropdown.choices = worldDatabase.allWorldPresets.Select(p => p.presetName).ToList();
        presetDropdown.index = 0; // Default to the first preset
    }

    private void HandleCreateNewWorld()
    {
        string worldName = newWorldNameInput.value;
        if (string.IsNullOrWhiteSpace(worldName)) return;

        // Get the selected preset from the dropdown and use it
        WorldPreset selectedPreset = worldDatabase.allWorldPresets[presetDropdown.index];
        worldManager.GenerateNewWorld(selectedPreset);
        
        populationManager.ClearSimulation();
        DatabaseManager.Instance.SaveWorld(worldName);
        
        newWorldNameInput.value = "";
        CloseMenuAndResume(worldName);
    }
    
    private void HandleLoadWorld(string worldName)
    {
        DatabaseManager.Instance.LoadWorld(worldName);
        CloseMenuAndResume(worldName);
    }

    public void HandleSaveCurrentWorld()
    {
        if (string.IsNullOrEmpty(currentWorldName)) return;
        DatabaseManager.Instance.SaveWorld(currentWorldName);
    }

    private void HandleDeleteWorld(string worldName)
    {
        DatabaseManager.Instance.DeleteWorld(worldName);
        if (worldName == currentWorldName) currentWorldName = null;
        PopulateWorldList();
    }



    private void CloseMenuAndResume(string newWorldName)
    {
        currentWorldName = newWorldName;
        gameController.SetCurrentWorld(newWorldName);
        menuRoot.style.display = DisplayStyle.None;
        gameController.ShowHUD();
        gameController.ResumeTime();
        menuRoot.Blur();
    }
}