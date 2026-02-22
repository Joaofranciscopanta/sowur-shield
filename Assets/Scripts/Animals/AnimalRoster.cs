using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized singleton registry tracking all owned animals in the scene.
/// Animals register/unregister themselves on Start/OnDestroy.
/// Provides filtering by type, zone, and family for UI and gameplay queries.
/// </summary>
public class AnimalRoster : MonoBehaviour
{
    public static AnimalRoster Instance { get; private set; }

    private List<Animal> registeredAnimals = new List<Animal>();

    /// <summary>Fired when an animal is added to the roster.</summary>
    public event Action<Animal> OnAnimalRegistered;

    /// <summary>Fired when an animal is removed from the roster.</summary>
    public event Action<Animal> OnAnimalUnregistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================================
    // Registration
    // =========================================================================

    /// <summary>Register an animal with the roster. Called by Animal.Start().</summary>
    public void RegisterAnimal(Animal animal)
    {
        if (animal == null) return;
        if (registeredAnimals.Contains(animal)) return;

        registeredAnimals.Add(animal);
        OnAnimalRegistered?.Invoke(animal);
    }

    /// <summary>Unregister an animal from the roster. Called by Animal.OnDestroy().</summary>
    public void UnregisterAnimal(Animal animal)
    {
        if (animal == null) return;
        if (!registeredAnimals.Contains(animal)) return;

        registeredAnimals.Remove(animal);
        OnAnimalUnregistered?.Invoke(animal);
    }

    // =========================================================================
    // Queries
    // =========================================================================

    /// <summary>Get a copy of all registered animals.</summary>
    public List<Animal> GetAllAnimals()
    {
        return new List<Animal>(registeredAnimals);
    }

    /// <summary>Total number of registered animals.</summary>
    public int GetAnimalCount()
    {
        return registeredAnimals.Count;
    }

    /// <summary>Filter animals by their AnimalData.animalType (e.g., "Chicken", "Cow").</summary>
    public List<Animal> GetAnimalsByType(string animalType)
    {
        if (string.IsNullOrEmpty(animalType)) return new List<Animal>();
        return registeredAnimals.Where(a =>
            a != null && a.AnimalData != null &&
            string.Equals(a.AnimalData.animalType, animalType, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>Filter animals by their assigned AnimalZone.</summary>
    public List<Animal> GetAnimalsByZone(AnimalZone zone)
    {
        if (zone == null) return new List<Animal>();
        return registeredAnimals.Where(a =>
            a != null && a.AssignedZone == zone
        ).ToList();
    }

    /// <summary>
    /// Count animals of a specific biological family (e.g., "Bovidae", "Phasianidae").
    /// Used by AnimalSkill unlock conditions (FamilyCount type).
    /// </summary>
    public int GetFamilyCount(string family)
    {
        if (string.IsNullOrEmpty(family)) return 0;
        return registeredAnimals.Count(a =>
            a != null && a.AnimalData != null &&
            string.Equals(a.AnimalData.animalFamily, family, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>Get average happiness across all registered animals (0-100).</summary>
    public float GetAverageHappiness()
    {
        if (registeredAnimals.Count == 0) return 0f;
        float total = 0f;
        int count = 0;
        foreach (var animal in registeredAnimals)
        {
            if (animal != null)
            {
                total += animal.GetHappiness();
                count++;
            }
        }
        return count > 0 ? total / count : 0f;
    }

    /// <summary>Count animals that still need feeding today.</summary>
    public int GetHungryAnimalCount()
    {
        return registeredAnimals.Count(a => a != null && a.NeedsFeeding);
    }

    /// <summary>Get all unique zones that have registered animals.</summary>
    public List<AnimalZone> GetOccupiedZones()
    {
        var zones = new HashSet<AnimalZone>();
        foreach (var animal in registeredAnimals)
        {
            if (animal != null && animal.AssignedZone != null)
                zones.Add(animal.AssignedZone);
        }
        return new List<AnimalZone>(zones);
    }
}
