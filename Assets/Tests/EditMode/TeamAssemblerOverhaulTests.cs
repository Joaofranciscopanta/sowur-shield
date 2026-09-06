using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Animals;
using SowurShield.Combat;
using SowurShield.Core;

namespace SowurShield.Tests
{

/// <summary>
/// Regression tests for the team assembler overhaul (2026-09-06).
///
/// Each of these covers a defect that produced no console error and no failing test:
/// the screen advertised one synergy rule while the battle applied another, family
/// synergies counted the whole farm instead of the assembled team, and two animals of
/// the same species were treated as one animal.
///
/// Every test here was verified by re-introducing the original behaviour and watching it
/// fail — a test never seen red is not known to test anything.
/// </summary>
public class TeamAssemblerOverhaulTests
{
    private readonly List<Object> cleanup = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        cleanup.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in cleanup)
            if (obj != null) Object.DestroyImmediate(obj);
        cleanup.Clear();

        // The team is a persistent singleton — leaving entries behind leaks into the
        // next test.
        TeamAssemblerData.Instance.team.Clear();
    }

    private static TeamSynergy.Member Member(string family, string combatClass = "DPS",
        float happiness = 50f, bool fedPreferred = false, int x = 6, int y = 2)
    {
        return new TeamSynergy.Member
        {
            family = family,
            combatClass = combatClass,
            happiness = happiness,
            fedPreferredFood = fedPreferred,
            gridPosition = new Vector2Int(x, y)
        };
    }

    // ── Flock ─────────────────────────────────────────────────────────────────

    [Test]
    public void Flock_RequiresThreeOfTheSameFamily()
    {
        var two = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Galliformes")
        };
        Assert.IsFalse(TeamSynergy.Evaluate(two).Any(s => s.type == SynergyType.Flock),
            "Two of a family must not trigger Flock.");

        var three = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Galliformes"), Member("Galliformes")
        };
        var flock = TeamSynergy.Evaluate(three).FirstOrDefault(s => s.type == SynergyType.Flock);
        Assert.IsNotNull(flock, "Three of a family must trigger Flock.");
        Assert.AreEqual("Galliformes", flock.subject);
        Assert.AreEqual(3, flock.count);
        Assert.Greater(flock.attackMultiplier, 1f);
    }

    [Test]
    public void Flock_ScalesAtFiveMembers()
    {
        var three = Enumerable.Range(0, 3).Select(_ => Member("Bovidae")).ToList();
        var five = Enumerable.Range(0, 5).Select(_ => Member("Bovidae")).ToList();

        float smallBonus = TeamSynergy.Evaluate(three)
            .First(s => s.type == SynergyType.Flock).attackMultiplier;
        float largeBonus = TeamSynergy.Evaluate(five)
            .First(s => s.type == SynergyType.Flock).attackMultiplier;

        Assert.Greater(largeBonus, smallBonus, "A flock of five should beat a flock of three.");
    }

    [Test]
    public void Flock_OnlyBuffsItsOwnFamily()
    {
        var team = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Galliformes"), Member("Galliformes"),
            Member("Bovidae", "Tank")
        };

        var flock = TeamSynergy.Evaluate(team).First(s => s.type == SynergyType.Flock);

        Assert.IsTrue(TeamSynergy.AppliesTo(flock, Member("Galliformes"), 6),
            "A chicken must receive the chicken flock bonus.");
        Assert.IsFalse(TeamSynergy.AppliesTo(flock, Member("Bovidae", "Tank"), 6),
            "A cow must not receive the chicken flock bonus.");
    }

    // ── Mixed Yard ────────────────────────────────────────────────────────────

    [Test]
    public void MixedYard_RequiresThreeDistinctFamilies()
    {
        var twoFamilies = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Bovidae", "Tank")
        };
        Assert.IsFalse(TeamSynergy.Evaluate(twoFamilies).Any(s => s.type == SynergyType.MixedYard));

        var threeFamilies = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Bovidae", "Tank"), Member("Anatidae")
        };
        var mixed = TeamSynergy.Evaluate(threeFamilies)
            .FirstOrDefault(s => s.type == SynergyType.MixedYard);

        Assert.IsNotNull(mixed, "Three distinct families must trigger Mixed Yard.");
        Assert.Greater(mixed.healthMultiplier, 1f);
    }

    [Test]
    public void MixedYard_CounterbalancesFlock()
    {
        // The design point: stacking one family and spreading across families must both
        // be viable, or team building has no decision in it.
        var stacked = Enumerable.Range(0, 3).Select(_ => Member("Galliformes")).ToList();
        var spread = new List<TeamSynergy.Member>
        {
            Member("Galliformes"), Member("Bovidae", "Tank"), Member("Anatidae")
        };

        Assert.IsTrue(TeamSynergy.Evaluate(stacked).Any(s => s.type == SynergyType.Flock));
        Assert.IsTrue(TeamSynergy.Evaluate(spread).Any(s => s.type == SynergyType.MixedYard));
    }

    // ── Front Line ────────────────────────────────────────────────────────────

    [Test]
    public void FrontLine_RequiresTwoTanksInTheFrontColumn()
    {
        // Front column is the lowest occupied x: enemies come from the left.
        var oneTank = new List<TeamSynergy.Member>
        {
            Member("Bovidae", "Tank", x: 6),
            Member("Galliformes", "DPS", x: 6),
            Member("Galliformes", "DPS", x: 7)
        };
        Assert.IsFalse(TeamSynergy.Evaluate(oneTank).Any(s => s.type == SynergyType.FrontLine));

        var twoTanks = new List<TeamSynergy.Member>
        {
            Member("Bovidae", "Tank", x: 6, y: 1),
            Member("Bovidae", "Tank", x: 6, y: 3),
            Member("Galliformes", "DPS", x: 7)
        };
        var front = TeamSynergy.Evaluate(twoTanks)
            .FirstOrDefault(s => s.type == SynergyType.FrontLine);

        Assert.IsNotNull(front, "Two tanks in the front column must trigger Front Line.");
        Assert.Less(front.damageTakenMultiplier, 1f);
    }

    [Test]
    public void FrontLine_ProtectsTheRanksBehind_NotTheTanksThemselves()
    {
        var team = new List<TeamSynergy.Member>
        {
            Member("Bovidae", "Tank", x: 6, y: 1),
            Member("Bovidae", "Tank", x: 6, y: 3),
            Member("Galliformes", "DPS", x: 8)
        };
        var front = TeamSynergy.Evaluate(team).First(s => s.type == SynergyType.FrontLine);

        Assert.IsFalse(TeamSynergy.AppliesTo(front, Member("Bovidae", "Tank", x: 6), 6),
            "The shield does not protect the shield.");
        Assert.IsTrue(TeamSynergy.AppliesTo(front, Member("Galliformes", "DPS", x: 8), 6),
            "Units behind the front line are the ones protected.");
    }

    // ── Well Cared / Well Fed ─────────────────────────────────────────────────

    [Test]
    public void WellCared_RequiresEveryMemberAboveThreshold()
    {
        float high = TeamSynergy.WellCaredMinHappiness + 5f;
        float low = TeamSynergy.WellCaredMinHappiness - 5f;

        var oneSad = new List<TeamSynergy.Member>
        {
            Member("Galliformes", happiness: high), Member("Bovidae", "Tank", happiness: low)
        };
        Assert.IsFalse(TeamSynergy.Evaluate(oneSad).Any(s => s.type == SynergyType.WellCared),
            "One unhappy animal must break the team-wide bonus.");

        var allHappy = new List<TeamSynergy.Member>
        {
            Member("Galliformes", happiness: high), Member("Bovidae", "Tank", happiness: high)
        };
        Assert.IsTrue(TeamSynergy.Evaluate(allHappy).Any(s => s.type == SynergyType.WellCared));
    }

    [Test]
    public void WellFed_RequiresPreferredFood_NotMerelyBeingFed()
    {
        var fedAnything = new List<TeamSynergy.Member>
        {
            Member("Galliformes", fedPreferred: false), Member("Bovidae", "Tank", fedPreferred: true)
        };
        Assert.IsFalse(TeamSynergy.Evaluate(fedAnything).Any(s => s.type == SynergyType.WellFed));

        var fedFavourites = new List<TeamSynergy.Member>
        {
            Member("Galliformes", fedPreferred: true), Member("Bovidae", "Tank", fedPreferred: true)
        };
        Assert.IsTrue(TeamSynergy.Evaluate(fedFavourites).Any(s => s.type == SynergyType.WellFed));
    }

    [Test]
    public void EmptyTeam_HasNoSynergies()
    {
        Assert.IsEmpty(TeamSynergy.Evaluate(new List<TeamSynergy.Member>()));
        Assert.IsEmpty(TeamSynergy.Evaluate(null));
    }

    // ── Food preferences ──────────────────────────────────────────────────────

    [Test]
    public void FoodPreference_DiffersBetweenFamilies()
    {
        // The original bug: 26 of 28 animals asked for the same CarrotSeed, so feeding
        // carried no decision at all.
        string cow = FoodPreference.GetPreferredFood(MakeData("Bovidae"));
        string chicken = FoodPreference.GetPreferredFood(MakeData("Galliformes"));
        string rabbit = FoodPreference.GetPreferredFood(MakeData("Leporidae"));

        Assert.IsNotEmpty(cow);
        Assert.IsNotEmpty(chicken);
        Assert.AreNotEqual(cow, chicken, "Cows and chickens must not want the same feed.");
        Assert.AreNotEqual(cow, rabbit);
    }

    [Test]
    public void FoodPreference_UnknownFamilyHasNoPreference()
    {
        Assert.IsEmpty(FoodPreference.GetPreferredFood(MakeData("Nonexistent")));
        Assert.IsEmpty(FoodPreference.GetPreferredFood(null));
    }

    [Test]
    public void FoodPreference_UnfedPenaltyIsAReduction()
    {
        // Feeding stopped being a hard gate; the penalty is what replaces it, so it has
        // to actually cost something.
        Assert.Less(FoodPreference.UnfedStatPenalty, 1f);
        Assert.Greater(FoodPreference.UnfedStatPenalty, 0f);
    }

    // ── Identity by individual, not by species ────────────────────────────────

    [Test]
    public void TwoAnimalsOfTheSameSpecies_AreTwoDifferentAnimals()
    {
        // The original bug: team membership keyed off AnimalData, the shared
        // ScriptableObject, so the second chicken could never join the team. It went
        // unnoticed only because the shipped farm gives every animal a distinct asset.
        var shared = MakeData("Galliformes");

        var first = MakeAnimal("Chicken_A", shared);
        var second = MakeAnimal("Chicken_B", shared);

        var data = TeamAssemblerData.Instance;
        data.ClearTeam();

        Assert.IsTrue(data.AddAnimal(first, new Vector2Int(6, 0)),
            "The first animal must join the team.");
        Assert.IsTrue(data.AddAnimal(second, new Vector2Int(6, 1)),
            "A second animal of the same species must also be able to join.");
        Assert.AreEqual(2, data.GetTeamSize());

        data.ClearTeam();
    }

    [Test]
    public void FeedingOneAnimal_DoesNotFeedItsTwin()
    {
        var shared = MakeData("Galliformes");
        var first = MakeAnimal("Chicken_C", shared);
        var second = MakeAnimal("Chicken_D", shared);

        var data = TeamAssemblerData.Instance;
        data.ClearTeam();
        data.AddAnimal(first, new Vector2Int(6, 0));
        data.AddAnimal(second, new Vector2Int(6, 1));

        data.MarkAsFed(first);

        Assert.IsTrue(data.FindMember(first).isFed, "The fed animal must be marked fed.");
        Assert.IsFalse(data.FindMember(second).isFed,
            "Feeding one animal must not mark its same-species twin as fed.");

        data.ClearTeam();
    }

    [Test]
    public void RemovingOneAnimal_LeavesItsTwinInTheTeam()
    {
        var shared = MakeData("Galliformes");
        var first = MakeAnimal("Chicken_E", shared);
        var second = MakeAnimal("Chicken_F", shared);

        var data = TeamAssemblerData.Instance;
        data.ClearTeam();
        data.AddAnimal(first, new Vector2Int(6, 0));
        data.AddAnimal(second, new Vector2Int(6, 1));

        data.RemoveAnimal(first);

        Assert.AreEqual(1, data.GetTeamSize());
        Assert.IsTrue(data.IsAnimalInTeam(second), "The twin must stay on the team.");

        data.ClearTeam();
    }

    // ── Feeding is no longer a gate ───────────────────────────────────────────

    [Test]
    public void TeamWithHungryAnimals_IsStillValid()
    {
        // Feeding used to block the battle button with no explanation. Now an unfed
        // animal fights at a penalty instead.
        var data = TeamAssemblerData.Instance;
        data.ClearTeam();

        var animal = MakeAnimal("Hungry_One", MakeData("Galliformes"));
        data.AddAnimal(animal, new Vector2Int(6, 0));

        Assert.IsFalse(data.AreAllAnimalsFed(), "Precondition: the animal is unfed.");
        Assert.IsTrue(data.IsTeamValid(), "A hungry team must still be allowed to fight.");
        Assert.AreEqual(1, data.GetUnfedCount());

        data.ClearTeam();
    }

    [Test]
    public void EmptyTeam_IsNotValid()
    {
        var data = TeamAssemblerData.Instance;
        data.ClearTeam();
        Assert.IsFalse(data.IsTeamValid(), "An empty team must not be able to start a battle.");
    }

    // ── Team profiles ─────────────────────────────────────────────────────────

    [Test]
    public void LoadingASaveWithNoProfiles_DoesNotWipeProfilesMadeThisSession()
    {
        // SaveManager.RegisterSaveable replays LoadData onto anything registering after
        // the initial load. LoadData used to Clear() before checking whether the save had
        // any profile data at all, so a profile saved seconds earlier vanished — no error,
        // no warning. Measured in play mode: saved 1 profile, read back 0.
        var manager = Track(new GameObject("TeamProfileManagerTest"))
            .AddComponent<TeamProfileManager>();

        var data = TeamAssemblerData.Instance;
        data.ClearTeam();
        data.AddAnimal(MakeAnimal("ProfileAnimal", MakeData("Galliformes")), new Vector2Int(6, 0));

        Assert.IsNotNull(manager.SaveCurrentTeam("Session Team"), "Precondition: profile saved.");
        Assert.AreEqual(1, manager.Profiles.Count);

        // An empty save, exactly as a fresh slot would supply.
        manager.LoadData(new GameData());

        Assert.AreEqual(1, manager.Profiles.Count,
            "A save carrying no profile data must not delete profiles held in memory.");

        data.ClearTeam();
    }

    [Test]
    public void AProfileRoundTripsThroughSaveData()
    {
        var manager = Track(new GameObject("TeamProfileRoundTrip")).AddComponent<TeamProfileManager>();

        var data = TeamAssemblerData.Instance;
        data.ClearTeam();
        data.AddAnimal(MakeAnimal("RoundTripCow", MakeData("Bovidae")), new Vector2Int(7, 3));
        manager.SaveCurrentTeam("Round Trip");

        var saved = new GameData();
        manager.SaveData(saved);

        var reloaded = Track(new GameObject("TeamProfileReloaded")).AddComponent<TeamProfileManager>();
        reloaded.LoadData(saved);

        Assert.AreEqual(1, reloaded.Profiles.Count, "The profile must survive a save/load cycle.");
        Assert.AreEqual("Round Trip", reloaded.Profiles[0].profileName);
        Assert.AreEqual(1, reloaded.Profiles[0].entries.Count);
        Assert.AreEqual(new Vector2Int(7, 3), reloaded.Profiles[0].entries[0].Position,
            "Grid position must round-trip, not just membership.");

        data.ClearTeam();
    }

    private Animal MakeAnimal(string uniqueName, AnimalData data)
    {
        var go = Track(new GameObject(uniqueName));
        var animal = go.AddComponent<Animal>();

        // animalData is a private SerializeField; the spawner sets it the same way.
        typeof(Animal)
            .GetField("animalData", System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance)
            ?.SetValue(animal, data);

        return animal;
    }

    private AnimalData MakeData(string family)
    {
        var data = Track(ScriptableObject.CreateInstance<AnimalData>());
        data.animalFamily = family;
        return data;
    }
}

} // namespace SowurShield.Tests
