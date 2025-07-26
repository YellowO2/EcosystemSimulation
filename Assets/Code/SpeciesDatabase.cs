using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpeciesDatabase", menuName = "Simulation/Species Database")]
public class SpeciesDatabase : ScriptableObject
{
    public List<SpeciesConfiguration> allSpecies;
}