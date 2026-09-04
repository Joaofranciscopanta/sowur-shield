using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Dialogue;

namespace SowurShield.Dialogue.Editor
{

[CustomEditor(typeof(DialogueTree))]
public class DialogueTreeEditor : UnityEditor.Editor
{
    private DialogueTree dialogueTree;
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private bool showNodeDetails = true;
#pragma warning disable CS0414
    private bool showValidationResults = false;
#pragma warning restore CS0414
    private string newNodeId = "";
    private NodeType newNodeType = NodeType.Dialogue;
    private List<bool> nodeFoldouts = new List<bool>();
    
    private SerializedProperty conversationIdProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty startNodeIdProp;
    private SerializedProperty isRepeatableProp;
    private SerializedProperty saveProgressProp;
    private SerializedProperty priorityProp;
    private SerializedProperty nodesProp;
    
    private void OnEnable()
    {
        dialogueTree = (DialogueTree)target;
        
        // Get serialized properties
        conversationIdProp = serializedObject.FindProperty("conversationId");
        descriptionProp = serializedObject.FindProperty("conversationDescription");
        startNodeIdProp = serializedObject.FindProperty("startNodeId");
        isRepeatableProp = serializedObject.FindProperty("isRepeatable");
        saveProgressProp = serializedObject.FindProperty("saveProgressOnExit");
        priorityProp = serializedObject.FindProperty("priority");
        nodesProp = serializedObject.FindProperty("nodes");
        
        // Initialize foldouts
        RefreshFoldouts();
    }
    
    private void RefreshFoldouts()
    {
        if (dialogueTree.nodes != null)
        {
            while (nodeFoldouts.Count < dialogueTree.nodes.Length)
                nodeFoldouts.Add(false);
            while (nodeFoldouts.Count > dialogueTree.nodes.Length)
                nodeFoldouts.RemoveAt(nodeFoldouts.Count - 1);
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Tree Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        DrawBasicProperties();
        EditorGUILayout.Space();
        
        DrawValidationSection();
        EditorGUILayout.Space();
        
        DrawNodeManagement();
        EditorGUILayout.Space();
        
        DrawNodesSection();
        EditorGUILayout.Space();
        
        DrawAdvancedOptions();
        
        serializedObject.ApplyModifiedProperties();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(dialogueTree);
        }
    }
    
    private void DrawBasicProperties()
    {
        EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(conversationIdProp, new GUIContent("Conversation ID"));
        
        EditorGUILayout.LabelField("Description:");
        EditorGUILayout.PropertyField(descriptionProp, GUIContent.none, GUILayout.Height(60));
        
        EditorGUILayout.PropertyField(startNodeIdProp, new GUIContent("Start Node ID"));
        EditorGUILayout.PropertyField(isRepeatableProp, new GUIContent("Is Repeatable"));
        EditorGUILayout.PropertyField(saveProgressProp, new GUIContent("Save Progress On Exit"));
        EditorGUILayout.PropertyField(priorityProp, new GUIContent("Priority"));
    }
    
    private void DrawValidationSection()
    {
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Tree", GUILayout.Width(120)))
        {
            bool isValid = dialogueTree.ValidateTree();
            showValidationResults = true;
            
            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation Result", "Dialogue tree is valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Result", 
                    "Dialogue tree has validation errors! Check the console for details.", "OK");
            }
        }
        
        if (GUILayout.Button("Open Node Editor", GUILayout.Width(120)))
        {
            DialogueNodeEditorWindow.ShowWindow(dialogueTree);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Show validation summary
        if (dialogueTree.nodes != null)
        {
            EditorGUILayout.LabelField($"Total Nodes: {dialogueTree.nodes.Length}");
            var reachableNodes = dialogueTree.GetReachableNodes();
            EditorGUILayout.LabelField($"Reachable Nodes: {reachableNodes.Length}");
            
            if (reachableNodes.Length < dialogueTree.nodes.Length)
            {
                EditorGUILayout.HelpBox(
                    $"{dialogueTree.nodes.Length - reachableNodes.Length} nodes are unreachable from start node!", 
                    MessageType.Warning
                );
            }
        }
    }
    
    private void DrawNodeManagement()
    {
        EditorGUILayout.LabelField("Node Management", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        newNodeId = EditorGUILayout.TextField("New Node ID", newNodeId);
        newNodeType = (NodeType)EditorGUILayout.EnumPopup("Type", newNodeType);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Node"))
        {
            AddNewNode();
        }
        
        if (GUILayout.Button("Sort Nodes"))
        {
            SortNodes();
        }
        
        if (GUILayout.Button("Clean Up"))
        {
            CleanUpNodes();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawNodesSection()
    {
        if (nodesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No nodes in this dialogue tree. Add nodes to get started!", MessageType.Info);
            return;
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Nodes ({nodesProp.arraySize})", EditorStyles.boldLabel);
        showNodeDetails = EditorGUILayout.Toggle("Show Details", showNodeDetails);
        EditorGUILayout.EndHorizontal();
        
        RefreshFoldouts();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        for (int i = 0; i < nodesProp.arraySize; i++)
        {
            DrawNodeEditor(i);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawNodeEditor(int index)
    {
        SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
        if (nodeProp == null) return;
        
        SerializedProperty nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
        string nodeId = nodeIdProp != null ? nodeIdProp.stringValue : $"Node {index}";
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // Node header with foldout
        EditorGUILayout.BeginHorizontal();
        
        if (index < nodeFoldouts.Count)
        {
            nodeFoldouts[index] = EditorGUILayout.Foldout(nodeFoldouts[index], $"[{index}] {nodeId}", true);
        }
        
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            RemoveNode(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();
        
        // Show node details if expanded
        if (index < nodeFoldouts.Count && nodeFoldouts[index])
        {
            EditorGUI.indentLevel++;
            
            if (showNodeDetails)
            {
                DrawDetailedNodeEditor(nodeProp);
            }
            else
            {
                DrawCompactNodeEditor(nodeProp);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void DrawDetailedNodeEditor(SerializedProperty nodeProp)
    {
        // Basic properties
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("nodeId"), new GUIContent("Node ID"));
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("nodeType"), new GUIContent("Type"));
        
        EditorGUILayout.Space();
        
        // Speaker info
        EditorGUILayout.LabelField("Speaker", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("speakerName"), new GUIContent("Name"));
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("speakerPortrait"), new GUIContent("Portrait"));
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("speakerEmotion"), new GUIContent("Emotion"));
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("speakerPosition"), new GUIContent("Position"));
        
        EditorGUILayout.Space();
        
        // Texto do dialogo: um campo por idioma, escrito direto nas tabelas.
        //
        // Isto era um TextArea escrevendo no stringValue do campo, e nao funcionava:
        // um LocalizedString num SerializedProperty e Generic, entao stringValue le
        // vazio e escrever nele nao grava nada — sem erro. Quem digitava uma fala
        // aqui a via sumir ao sair do campo.
        EditorGUILayout.LabelField("Texto do diálogo", EditorStyles.boldLabel);
        DesenharTextoLocalizado(nodeProp, 60f);
        
        EditorGUILayout.Space();
        
        // Navigation
        EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("nextNodeId"), new GUIContent("Next Node ID"));
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("autoAdvanceDelay"), new GUIContent("Auto Advance Delay"));
        
        EditorGUILayout.Space();
        
        // Arrays
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("choices"), new GUIContent("Choices"), true);
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("displayConditions"), new GUIContent("Display Conditions"), true);
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("nodeEffects"), new GUIContent("Node Effects"), true);
        
        EditorGUILayout.Space();
        
        // Editor note
        EditorGUILayout.LabelField("Editor Note", EditorStyles.boldLabel);
        var editorNoteProp = nodeProp.FindPropertyRelative("editorNote");
        editorNoteProp.stringValue = EditorGUILayout.TextArea(editorNoteProp.stringValue, GUILayout.Height(40));
    }
    
    private void DrawCompactNodeEditor(SerializedProperty nodeProp)
    {
        // Show just essential info
        var speakerNameProp = nodeProp.FindPropertyRelative("speakerName");
        var nextNodeIdProp = nodeProp.FindPropertyRelative("nextNodeId");
        var choicesProp = nodeProp.FindPropertyRelative("choices");
        
        EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("nodeId"), new GUIContent("Node ID"));
        EditorGUILayout.PropertyField(speakerNameProp, new GUIContent("Speaker"));
        
        EditorGUILayout.LabelField("Texto do diálogo:");
        DesenharTextoLocalizado(nodeProp, 40f);
        
        EditorGUILayout.PropertyField(nextNodeIdProp, new GUIContent("Next Node ID"));
        
        if (choicesProp.arraySize > 0)
        {
            EditorGUILayout.LabelField($"Choices: {choicesProp.arraySize}");
        }
    }
    
    private void DrawAdvancedOptions()
    {
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.LabelField("Import/Export", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export to JSON"))
            {
                ExportToJSON();
            }
            
            if (GUILayout.Button("Import from JSON"))
            {
                ImportFromJSON();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("Auto-Generate Node IDs"))
            {
                AutoGenerateNodeIDs();
            }
            
            if (GUILayout.Button("Fix Broken References"))
            {
                FixBrokenReferences();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// Desenha o texto do no nos tres idiomas.
    ///
    /// Precisa do objeto real (nao do SerializedProperty) porque um LocalizedString
    /// so pode ser lido e escrito pela API dele; via SerializedProperty ele e um
    /// blob Generic. Por isso aplicamos o que estiver pendente antes e
    /// re-serializamos depois.
    /// </summary>
    private void DesenharTextoLocalizado(SerializedProperty nodeProp, float altura)
    {
        var arvore = target as DialogueTree;
        if (arvore == null) return;

        int indice = IndiceDoNo(nodeProp);
        if (indice < 0 || indice >= arvore.nodes.Length) return;

        var no = arvore.nodes[indice];
        serializedObject.ApplyModifiedProperties();

        string chaveSugerida = LocalizedTextField.ChavePadrao(arvore.name, no.nodeId);
        if (LocalizedTextField.Desenhar(no.dialogueText, chaveSugerida, altura))
        {
            EditorUtility.SetDirty(arvore);
        }

        serializedObject.Update();
    }

    /// <summary>
    /// O indice do no dentro do array, extraido do caminho do SerializedProperty
    /// ("nodes.Array.data[3]") — nao ha API direta para isso.
    /// </summary>
    private static int IndiceDoNo(SerializedProperty nodeProp)
    {
        string caminho = nodeProp.propertyPath;
        int abre = caminho.LastIndexOf('[');
        int fecha = caminho.LastIndexOf(']');
        if (abre < 0 || fecha <= abre) return -1;

        return int.TryParse(caminho.Substring(abre + 1, fecha - abre - 1), out int i) ? i : -1;
    }

    private void AddNewNode()
    {
        if (string.IsNullOrEmpty(newNodeId))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a node ID!", "OK");
            return;
        }
        
        // Check for duplicate ID
        if (dialogueTree.GetNode(newNodeId) != null)
        {
            EditorUtility.DisplayDialog("Error", $"Node with ID '{newNodeId}' already exists!", "OK");
            return;
        }
        
        // Add new element to array
        int newIndex = nodesProp.arraySize;
        nodesProp.arraySize++;
        
        SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(newIndex);
        
        // Set default values
        newNodeProp.FindPropertyRelative("nodeId").stringValue = newNodeId;
        newNodeProp.FindPropertyRelative("nodeType").enumValueIndex = (int)newNodeType;
        // Nada de `dialogueText.stringValue = ""` aqui: e um LocalizedString, e
        // escrever nele por stringValue nao faz nada. O no nasce sem chave, e ela
        // e criada quando se digita a primeira fala (ver LocalizedTextField).
        newNodeProp.FindPropertyRelative("speakerName").stringValue = "";
        newNodeProp.FindPropertyRelative("nextNodeId").stringValue = "";
        newNodeProp.FindPropertyRelative("autoAdvanceDelay").floatValue = 0f;
        newNodeProp.FindPropertyRelative("editorNote").stringValue = "";
        
        // Initialize arrays
        newNodeProp.FindPropertyRelative("choices").arraySize = 0;
        newNodeProp.FindPropertyRelative("displayConditions").arraySize = 0;
        newNodeProp.FindPropertyRelative("nodeEffects").arraySize = 0;
        
        // Add foldout for new node
        nodeFoldouts.Add(true); // Start expanded
        
        newNodeId = "";
        serializedObject.ApplyModifiedProperties();
    }
    
    private void RemoveNode(int index)
    {
        if (index < 0 || index >= nodesProp.arraySize) return;
        
        SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
        SerializedProperty nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
        string nodeId = nodeIdProp != null ? nodeIdProp.stringValue : $"Node {index}";
        
        bool confirmDelete = EditorUtility.DisplayDialog(
            "Confirm Delete", 
            $"Are you sure you want to delete node '{nodeId}'?", 
            "Yes", "No"
        );
        
        if (confirmDelete)
        {
            nodesProp.DeleteArrayElementAtIndex(index);
            
            // Remove corresponding foldout
            if (index < nodeFoldouts.Count)
                nodeFoldouts.RemoveAt(index);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    private void SortNodes()
    {
        if (dialogueTree.nodes == null) return;
        
        System.Array.Sort(dialogueTree.nodes, (a, b) => string.Compare(a.nodeId, b.nodeId));
        EditorUtility.SetDirty(dialogueTree);
    }
    
    private void CleanUpNodes()
    {
        if (dialogueTree.nodes == null) return;
        
        // Remove null nodes
        var cleanedNodes = dialogueTree.nodes.Where(n => n != null).ToArray();
        dialogueTree.nodes = cleanedNodes;
        
        // Remove duplicate IDs (keep first occurrence)
        var seenIds = new HashSet<string>();
        var uniqueNodes = new List<DialogueNode>();
        
        foreach (var node in cleanedNodes)
        {
            if (!seenIds.Contains(node.nodeId))
            {
                seenIds.Add(node.nodeId);
                uniqueNodes.Add(node);
            }
        }
        
        dialogueTree.nodes = uniqueNodes.ToArray();
        EditorUtility.SetDirty(dialogueTree);
        
        EditorUtility.DisplayDialog("Cleanup Complete", $"Cleaned up dialogue tree. {uniqueNodes.Count} nodes remain.", "OK");
    }
    
    private void AutoGenerateNodeIDs()
    {
        if (dialogueTree.nodes == null) return;
        
        for (int i = 0; i < dialogueTree.nodes.Length; i++)
        {
            if (string.IsNullOrEmpty(dialogueTree.nodes[i].nodeId))
            {
                dialogueTree.nodes[i].nodeId = $"node_{i:D3}";
            }
        }
        
        EditorUtility.SetDirty(dialogueTree);
    }
    
    private void FixBrokenReferences()
    {
        // This would implement logic to fix broken node references
        // For now, just show a message
        EditorUtility.DisplayDialog("Feature Coming Soon", "Automatic reference fixing is not yet implemented.", "OK");
    }
    
    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export Dialogue Tree", "", dialogueTree.name + ".json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(dialogueTree, true);
            System.IO.File.WriteAllText(path, json);
            EditorUtility.DisplayDialog("Export Complete", $"Exported to {path}", "OK");
        }
    }
    
    private void ImportFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import Dialogue Tree", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, dialogueTree);
                EditorUtility.SetDirty(dialogueTree);
                EditorUtility.DisplayDialog("Import Complete", $"Imported from {path}", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Failed to import: {e.Message}", "OK");
            }
        }
    }
}

} // namespace SowurShield.Dialogue.Editor