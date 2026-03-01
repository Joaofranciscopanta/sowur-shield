using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using SowurShield.Dialogue;

namespace SowurShield.Dialogue.Editor
{

public class DialogueMigrationUtility : EditorWindow
{
    private Vector2 scrollPosition;
    private string csvFilePath = "";
#pragma warning disable CS0414
    private bool includePortraits = true;
    private bool generateChoices = false;
#pragma warning restore CS0414
    private string defaultSpeakerName = "Unknown";
    private string outputFolderPath = "Assets/Data/Dialogue";

    // Legacy migration disabled - DialogueObject class removed
    // private List<DialogueObject> foundLegacyDialogues = new List<DialogueObject>();
    // Legacy NPC migration disabled - NPCInteractable class removed
    // private List<NPCInteractable> foundLegacyNPCs = new List<NPCInteractable>();
#pragma warning disable CS0414
    private bool hasScannedForLegacy = false;
#pragma warning restore CS0414
    
    [MenuItem("Window/Dialogue/Migration Utility")]
    public static void ShowWindow()
    {
        GetWindow<DialogueMigrationUtility>("Dialogue Migration Utility");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Dialogue System Migration Utility", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This utility helps you migrate from the legacy dialogue system to the new dialogue tree system.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        DrawLegacyMigrationSection();
        EditorGUILayout.Space();
        
        DrawCSVImportSection();
        EditorGUILayout.Space();
        
        DrawUtilitySection();
    }
    
    private void DrawLegacyMigrationSection()
    {
        EditorGUILayout.LabelField("Legacy Dialogue Migration", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "Legacy migration is disabled. Both DialogueObject and NPCInteractable classes have been removed. " +
            "The project now uses only the enhanced DialogueTree system with NPCDialogueInteractable components.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("✅ Current System:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• DialogueTree - Branching conversations");
        EditorGUILayout.LabelField("• NPCDialogueInteractable - Enhanced NPC interaction");
        EditorGUILayout.LabelField("• DialogueTreeUI - Complete UI with portraits & choices");
        EditorGUILayout.LabelField("• ConversationMemory - Persistent dialogue state");
    }
    
    private void DrawCSVImportSection()
    {
        EditorGUILayout.LabelField("CSV Import", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "CSV Format: NodeID,SpeakerName,DialogueText,NextNodeID,ChoiceText,ChoiceNextNodeID",
            MessageType.Info
        );
        
        EditorGUILayout.BeginHorizontal();
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
            if (!string.IsNullOrEmpty(path))
                csvFilePath = path;
        }
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(csvFilePath) && GUILayout.Button("Import from CSV"))
        {
            ImportFromCSV();
        }
    }
    
    private void DrawUtilitySection()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Example Tree"))
        {
            CreateExampleDialogueTree();
        }
        
        if (GUILayout.Button("Setup Scene"))
        {
            SetupDialogueScene();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Generate Documentation"))
        {
            GenerateDocumentation();
        }
    }
    
    private void ScanForLegacyDialogues()
    {
        // Legacy migration completely disabled - both DialogueObject and NPCInteractable classes removed
        // All legacy components have been removed from the project
        
        hasScannedForLegacy = true;
        

    }
    
    private void MigrateLegacyDialogues()
    {
        // DialogueObject migration disabled - class no longer exists
        EditorUtility.DisplayDialog("Migration Disabled", 
            "DialogueObject migration is disabled because the DialogueObject class has been removed. Use the new DialogueTree system directly.", "OK");
    }
    
    private void MigrateLegacyNPCs()
    {
        // NPC migration disabled - NPCInteractable class no longer exists
        EditorUtility.DisplayDialog("Migration Disabled", 
            "NPC migration is disabled because the legacy NPCInteractable class has been removed. Use NPCDialogueInteractable directly for new NPCs.", "OK");
    }
    
    private void ImportFromCSV()
    {
        if (!File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "CSV file not found!", "OK");
            return;
        }
        
        try
        {
            var lines = File.ReadAllLines(csvFilePath);
            var nodes = new List<DialogueNode>();
            
            // Skip header if present
            int startIndex = lines[0].StartsWith("NodeID") ? 1 : 0;
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 3) continue;
                
                var node = new DialogueNode
                {
                    nodeId = parts[0].Trim(),
                    speakerName = parts.Length > 1 ? parts[1].Trim() : defaultSpeakerName,
                    dialogueText = parts.Length > 2 ? parts[2].Trim() : "",
                    nextNodeId = parts.Length > 3 ? parts[3].Trim() : "",
                    nodeType = NodeType.Dialogue
                };
                
                // Handle choices if present
                if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4]))
                {
                    var choice = new DialogueChoice
                    {
                        choiceText = parts[4].Trim(),
                        nextNodeId = parts.Length > 5 ? parts[5].Trim() : ""
                    };
                    node.choices = new DialogueChoice[] { choice };
                    node.nodeType = NodeType.Choice;
                }
                
                nodes.Add(node);
            }
            
            // Create dialogue tree
            var tree = CreateInstance<DialogueTree>();
            tree.conversationId = $"imported_{Path.GetFileNameWithoutExtension(csvFilePath)}";
            tree.conversationDescription = $"Imported from {Path.GetFileName(csvFilePath)}";
            tree.startNodeId = nodes.Count > 0 ? nodes[0].nodeId : "start";
            tree.nodes = nodes.ToArray();
            
            // Save asset
            string assetPath = Path.Combine(outputFolderPath, $"{tree.conversationId}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(tree, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Import Complete", 
                $"Successfully imported dialogue tree with {nodes.Count} nodes", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Import Failed", $"Error importing CSV: {e.Message}", "OK");
        }
    }
    
    private void CreateExampleDialogueTree()
    {
        var tree = CreateInstance<DialogueTree>();
        tree.conversationId = "example_conversation";
        tree.conversationDescription = "Example dialogue tree demonstrating all features";
        tree.startNodeId = "greeting";
        
        var nodes = new List<DialogueNode>
        {
            new DialogueNode
            {
                nodeId = "greeting",
                speakerName = "Village Elder",
                dialogueText = "Hello, traveler! Welcome to our village. How can I help you?",
                nodeType = NodeType.Choice,
                choices = new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "I'm looking for work.",
                        nextNodeId = "work_offer"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Just passing through.",
                        nextNodeId = "farewell"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Tell me about this place.",
                        nextNodeId = "village_info"
                    }
                }
            },
            new DialogueNode
            {
                nodeId = "work_offer",
                speakerName = "Village Elder",
                dialogueText = "Ah, a working person! We always need help around here. There are monsters in the nearby forest that need dealing with.",
                nextNodeId = "quest_accept",
                nodeEffects = new DialogueEffect[]
                {
                    new DialogueEffect
                    {
                        effectType = EffectType.SetQuestStatus,
                        effectKey = "monster_hunt",
                        effectValue = "available"
                    }
                }
            },
            new DialogueNode
            {
                nodeId = "quest_accept",
                speakerName = "Village Elder",
                dialogueText = "Will you help us?",
                nodeType = NodeType.Choice,
                choices = new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Yes, I'll help!",
                        nextNodeId = "quest_accepted",
                        effects = new DialogueEffect[]
                        {
                            new DialogueEffect
                            {
                                effectType = EffectType.SetQuestStatus,
                                effectKey = "monster_hunt",
                                effectValue = "accepted"
                            },
                            new DialogueEffect
                            {
                                effectType = EffectType.ModifyRelationship,
                                effectKey = "village_elder",
                                numericValue = 10f
                            }
                        }
                    },
                    new DialogueChoice
                    {
                        choiceText = "Not right now.",
                        nextNodeId = "quest_declined"
                    }
                }
            },
            new DialogueNode
            {
                nodeId = "quest_accepted",
                speakerName = "Village Elder",
                dialogueText = "Excellent! Here, take this sword. It should help you deal with the monsters.",
                nextNodeId = "",
                nodeEffects = new DialogueEffect[]
                {
                    new DialogueEffect
                    {
                        effectType = EffectType.GiveItem,
                        effectKey = "iron_sword",
                        numericValue = 1
                    }
                }
            },
            new DialogueNode
            {
                nodeId = "quest_declined",
                speakerName = "Village Elder",
                dialogueText = "I understand. Perhaps another time. Safe travels!",
                nextNodeId = ""
            },
            new DialogueNode
            {
                nodeId = "village_info",
                speakerName = "Village Elder",
                dialogueText = "This is Riverside Village. We're a peaceful community of farmers and craftspeople, though we've been having trouble with monsters lately.",
                nextNodeId = "greeting"
            },
            new DialogueNode
            {
                nodeId = "farewell",
                speakerName = "Village Elder",
                dialogueText = "Safe travels, friend! Come back anytime.",
                nextNodeId = ""
            }
        };
        
        tree.nodes = nodes.ToArray();
        
        // Create output directory if needed
        if (!Directory.Exists(outputFolderPath))
            Directory.CreateDirectory(outputFolderPath);
        
        // Save as asset
        string assetPath = Path.Combine(outputFolderPath, "ExampleDialogueTree.asset");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(tree, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Example Created", 
            $"Example dialogue tree created at {assetPath}", "OK");
    }
    
    private void SetupDialogueScene()
    {
        // Create dialogue UI if it doesn't exist
        var dialogueUI = FindFirstObjectByType<DialogueTreeUI>();
        if (dialogueUI == null)
        {
            var uiObject = new GameObject("DialogueTreeUI");
            dialogueUI = uiObject.AddComponent<DialogueTreeUI>();
            // You would need to set up the UI components here
        }
        
        // Create conversation memory if it doesn't exist
        var conversationMemory = FindFirstObjectByType<ConversationMemory>();
        if (conversationMemory == null)
        {
            var memoryObject = new GameObject("ConversationMemory");
            conversationMemory = memoryObject.AddComponent<ConversationMemory>();
        }
        
        EditorUtility.DisplayDialog("Setup Complete", 
            "Dialogue system components have been added to the scene", "OK");
    }
    
    private void GenerateDocumentation()
    {
        string docPath = Path.Combine(Application.dataPath, "DialogueSystemDocumentation.md");

        var doc = new System.Text.StringBuilder();
        doc.AppendLine("# Enhanced Dialogue System Documentation");
        doc.AppendLine();
        doc.AppendLine("## Overview");
        doc.AppendLine("This document describes the enhanced dialogue system with branching conversations, character portraits, and conversation memory.");
        doc.AppendLine();
        doc.AppendLine("## Core Components");
        doc.AppendLine();
        doc.AppendLine("### DialogueTree");
        doc.AppendLine("- Main asset containing dialogue nodes and conversation metadata");
        doc.AppendLine("- Supports branching conversations with conditions and effects");
        doc.AppendLine();
        doc.AppendLine("### DialogueNode");
        doc.AppendLine("- Individual dialogue entries with speaker information");
        doc.AppendLine("- Supports choices, conditions, and effects");
        doc.AppendLine();
        doc.AppendLine("### ConversationMemory");
        doc.AppendLine("- Persistent storage for conversation state and choices");
        doc.AppendLine("- Tracks relationships and custom variables");
        doc.AppendLine();
        doc.AppendLine("## Getting Started");
        doc.AppendLine("1. Create a DialogueTree asset (Right-click → Create → Dialogue → Dialogue Tree)");
        doc.AppendLine("2. Open the Dialogue Tree Editor to design your conversation");
        doc.AppendLine("3. Assign the tree to an NPCDialogueInteractable component");
        doc.AppendLine("4. Test your dialogue in play mode");
        doc.AppendLine();
        doc.AppendLine("## Migration Guide");
        doc.AppendLine("Use the Migration Utility (Window → Dialogue → Migration Utility) to convert existing DialogueObject assets to the new format.");

        File.WriteAllText(docPath, doc.ToString());
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Documentation Generated",
            $"Documentation saved to {docPath}", "OK");
    }
}

} // namespace SowurShield.Dialogue.Editor