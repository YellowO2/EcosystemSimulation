// WorldDatabase.cs Contains a collection of world presets used in the simulation, 
// similar to SpeciesDatabase.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "WorldDatabase", menuName = "Simulation/World Database")]
public class WorldDatabase : ScriptableObject
{
    public List<WorldPreset> allWorldPresets;

    public WorldPreset FindPreset(string presetName)
    {
        return allWorldPresets.FirstOrDefault(p => p.presetName == presetName);
    }
}