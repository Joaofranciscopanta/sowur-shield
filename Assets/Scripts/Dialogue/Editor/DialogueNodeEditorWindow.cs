using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class DialogueNodeEditorWindow : EditorWindow
{
    private DialogueTree dialogueTree;
    private Vector2 scrollPosition;
    private Vector2 canvasOffset = Vector2.zero;
    private float zoomLevel = 1.0f;
    
    private DialogueNode selectedNode;
    private DialogueNode draggedNode;
    private Vector2 dragOffset;
    private bool isDragging = false;
    
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 100f;
    private const float MIN_ZOOM = 0.5f;
    private const float MAX_ZOOM = 2.0f;
    
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle startNodeStyle;
    
    [MenuItem("Window/Dialogue/Node Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueNodeEditorWindow>("Dialogue Node Editor");
    }
    
    public static void ShowWindow(DialogueTree tree)
    {
        var window = GetWindow<DialogueNodeEditorWindow>("Dialogue Node Editor");
        window.dialogueTree = tree;
        window.Focus();
    }
    
    private void OnEnable()
    {
        InitializeStyles();
    }
    
    private void InitializeStyles()
    {
        nodeStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(2, 2, Color.gray) },
            border = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(5, 5, 5, 5)
        };
        
        selectedNodeStyle = new GUIStyle(nodeStyle)
        {
            normal = { background = MakeTexture(2, 2, Color.yellow) }
        };
        
        startNodeStyle = new GUIStyle(nodeStyle)
        {
            normal = { background = MakeTexture(2, 2, Color.green) }
        };
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        var texture = new Texture2D(width, height);
        var colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = color;
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
    
    private void OnGUI()
    {
        HandleInput();
        
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawToolbar();
        EditorGUILayout.EndHorizontal();
        
        if (dialogueTree == null)
        {
            EditorGUILayout.HelpBox("Select a DialogueTree asset to edit.", MessageType.Info);
            return;
        }
        
        DrawCanvas();
        DrawInspector();
    }
    
    private void DrawToolbar()
    {
        // Tree selection
        var newTree = (DialogueTree)EditorGUILayout.ObjectField(
            dialogueTree, typeof(DialogueTree), false, GUILayout.Width(200));
        if (newTree != dialogueTree)
        {
            dialogueTree = newTree;
            selectedNode = null;
        }
        
        GUILayout.FlexibleSpace();
        
        // Zoom controls
        GUILayout.Label($"Zoom: {zoomLevel:F1}x");
        zoomLevel = GUILayout.HorizontalSlider(zoomLevel, MIN_ZOOM, MAX_ZOOM, GUILayout.Width(100));
        
        if (GUILayout.Button("Reset View", GUILayout.Width(80)))
        {
            canvasOffset = Vector2.zero;
            zoomLevel = 1.0f;
        }
        
        // Validation
        if (GUILayout.Button("Validate", GUILayout.Width(80)))
        {
            if (dialogueTree != null)
            {
                bool isValid = dialogueTree.ValidateTree();
                string message = isValid ? "Tree is valid!" : "Tree has validation errors!";
                EditorUtility.DisplayDialog("Validation Result", message, "OK");
            }
        }
    }
    
    private void HandleInput()
    {
        var e = Event.current;
        
        // Handle zoom with scroll wheel
        if (e.type == EventType.ScrollWheel)
        {
            float delta = -e.delta.y * 0.1f;
            zoomLevel = Mathf.Clamp(zoomLevel + delta, MIN_ZOOM, MAX_ZOOM);
            e.Use();
            Repaint();
        }
        
        // Handle canvas panning with middle mouse
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            canvasOffset += e.delta;
            e.Use();
            Repaint();
        }
    }
    
    private void DrawCanvas()
    {
        if (dialogueTree == null || dialogueTree.nodes == null) return;
        
        Rect canvasRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        GUI.BeginGroup(canvasRect);
        
        // Draw grid background
        DrawGrid(canvasRect);
        
        // Apply zoom and offset
        var matrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(canvasOffset, Quaternion.identity, Vector3.one * zoomLevel);
        
        // Draw connections first (so they appear behind nodes)
        DrawConnections();
        
        // Draw nodes
        for (int i = 0; i < dialogueTree.nodes.Length; i++)
        {
            DrawNode(dialogueTree.nodes[i], i);
        }
        
        GUI.matrix = matrix;
        GUI.EndGroup();
        
        // Handle node interaction
        HandleNodeInteraction(canvasRect);
    }
    
    private void DrawGrid(Rect rect)
    {
        int gridSize = 20;
        Color gridColor = Color.gray * 0.3f;
        
        Handles.BeginGUI();
        Handles.color = gridColor;
        
        // Vertical lines
        for (float x = rect.x; x < rect.width; x += gridSize)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, rect.height));
        }
        
        // Horizontal lines
        for (float y = rect.y; y < rect.height; y += gridSize)
        {
            Handles.DrawLine(new Vector3(0, y), new Vector3(rect.width, y));
        }
        
        Handles.EndGUI();
    }
    
    private void DrawConnections()
    {
        if (dialogueTree?.nodes == null) return;
        
        Handles.BeginGUI();
        
        foreach (var node in dialogueTree.nodes)
        {
            if (node == null) continue;
            
            Vector2 nodePos = GetNodePosition(node);
            Vector2 nodeCenter = nodePos + new Vector2(NODE_WIDTH * 0.5f, NODE_HEIGHT * 0.5f);
            
            // Draw connection to next node
            if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                var nextNode = dialogueTree.GetNode(node.nextNodeId);
                if (nextNode != null)
                {
                    Vector2 nextNodePos = GetNodePosition(nextNode);
                    Vector2 nextNodeCenter = nextNodePos + new Vector2(NODE_WIDTH * 0.5f, NODE_HEIGHT * 0.5f);
                    
                    Handles.color = Color.white;
                    DrawArrow(nodeCenter, nextNodeCenter);
                }
            }
            
            // Draw connections to choice nodes
            if (node.choices != null)
            {
                foreach (var choice in node.choices)
                {
                    if (choice != null && !string.IsNullOrEmpty(choice.nextNodeId))
                    {
                        var choiceNode = dialogueTree.GetNode(choice.nextNodeId);
                        if (choiceNode != null)
                        {
                            Vector2 choiceNodePos = GetNodePosition(choiceNode);
                            Vector2 choiceNodeCenter = choiceNodePos + new Vector2(NODE_WIDTH * 0.5f, NODE_HEIGHT * 0.5f);
                            
                            Handles.color = Color.cyan;
                            DrawArrow(nodeCenter, choiceNodeCenter);
                        }
                    }
                }
            }
        }
        
        Handles.EndGUI();
    }
    
    private void DrawArrow(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        Vector2 arrowHead = to - direction * 10f;
        
        Handles.DrawLine(from, arrowHead);
        
        // Draw arrow head
        Vector2 perp = new Vector2(-direction.y, direction.x) * 5f;
        Handles.DrawLine(arrowHead, to);
        Handles.DrawLine(arrowHead + perp * 0.5f, to);
        Handles.DrawLine(arrowHead - perp * 0.5f, to);
    }
    
    private void DrawNode(DialogueNode node, int index)
    {
        if (node == null) return;
        
        Vector2 nodePos = GetNodePosition(node);
        Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, NODE_HEIGHT);
        
        // Choose style based on node state
        GUIStyle style = nodeStyle;
        if (node == selectedNode)
            style = selectedNodeStyle;
        else if (node.nodeId == dialogueTree.startNodeId)
            style = startNodeStyle;
        
        // Draw node background
        GUI.Box(nodeRect, "", style);
        
        // Draw node content
        GUILayout.BeginArea(nodeRect);
        
        EditorGUILayout.LabelField(node.nodeId, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Type: {node.nodeType}");
        
        if (!string.IsNullOrEmpty(node.speakerName))
            EditorGUILayout.LabelField($"Speaker: {node.speakerName}");
        
        if (!string.IsNullOrEmpty(node.dialogueText))
        {
            string preview = node.dialogueText.Length > 30 ? 
                node.dialogueText.Substring(0, 30) + "..." : 
                node.dialogueText;
            EditorGUILayout.LabelField(preview, EditorStyles.wordWrappedLabel);
        }
        
        GUILayout.EndArea();
        
        // Handle node selection
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            if (nodeRect.Contains(Event.current.mousePosition))
            {
                selectedNode = node;
                draggedNode = node;
                dragOffset = Event.current.mousePosition - nodePos;
                isDragging = false;
                Event.current.Use();
                Repaint();
            }
        }
    }
    
    private Vector2 GetNodePosition(DialogueNode node)
    {
        // Use stored editor position, or calculate automatic layout
        if (node.editorPosition != Vector2.zero)
            return node.editorPosition;
        
        // Auto-layout: arrange nodes in a simple grid
        int index = System.Array.IndexOf(dialogueTree.nodes, node);
        int nodesPerRow = Mathf.Max(1, Mathf.FloorToInt(position.width / (NODE_WIDTH + 50)));
        int row = index / nodesPerRow;
        int col = index % nodesPerRow;
        
        return new Vector2(col * (NODE_WIDTH + 50) + 50, row * (NODE_HEIGHT + 50) + 50);
    }
    
    private void HandleNodeInteraction(Rect canvasRect)
    {
        var e = Event.current;
        
        // Handle node dragging
        if (draggedNode != null)
        {
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                isDragging = true;
                Vector2 worldMousePos = (e.mousePosition - canvasOffset) / zoomLevel;
                draggedNode.editorPosition = worldMousePos - dragOffset;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                draggedNode = null;
                isDragging = false;
                e.Use();
            }
        }
        
        // Handle canvas clicks (deselect nodes)
        if (e.type == EventType.MouseDown && e.button == 0 && !isDragging)
        {
            selectedNode = null;
            Repaint();
        }
    }
    
    private void DrawInspector()
    {
        if (selectedNode == null) return;
        
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
        EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // Basic properties
        selectedNode.nodeId = EditorGUILayout.TextField("Node ID", selectedNode.nodeId);
        selectedNode.nodeType = (NodeType)EditorGUILayout.EnumPopup("Type", selectedNode.nodeType);
        
        EditorGUILayout.Space();
        
        // Speaker info
        EditorGUILayout.LabelField("Speaker", EditorStyles.boldLabel);
        selectedNode.speakerName = EditorGUILayout.TextField("Name", selectedNode.speakerName);
        selectedNode.speakerPortrait = (Sprite)EditorGUILayout.ObjectField("Portrait", selectedNode.speakerPortrait, typeof(Sprite), false);
        selectedNode.speakerEmotion = (EmotionType)EditorGUILayout.EnumPopup("Emotion", selectedNode.speakerEmotion);
        selectedNode.speakerPosition = (SpeakerPosition)EditorGUILayout.EnumPopup("Position", selectedNode.speakerPosition);
        
        EditorGUILayout.Space();
        
        // Dialogue text
        EditorGUILayout.LabelField("Dialogue Text", EditorStyles.boldLabel);
        selectedNode.dialogueText = EditorGUILayout.TextArea(selectedNode.dialogueText, GUILayout.Height(80));
        
        EditorGUILayout.Space();
        
        // Navigation
        EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
        selectedNode.nextNodeId = EditorGUILayout.TextField("Next Node ID", selectedNode.nextNodeId);
        selectedNode.autoAdvanceDelay = EditorGUILayout.FloatField("Auto Advance Delay", selectedNode.autoAdvanceDelay);
        
        EditorGUILayout.Space();
        
        // Quick actions
        if (GUILayout.Button("Set as Start Node"))
        {
            dialogueTree.startNodeId = selectedNode.nodeId;
            EditorUtility.SetDirty(dialogueTree);
        }
        
        if (GUILayout.Button("Delete Node"))
        {
            if (EditorUtility.DisplayDialog("Confirm Delete", $"Delete node '{selectedNode.nodeId}'?", "Yes", "No"))
            {
                var nodeList = new List<DialogueNode>(dialogueTree.nodes);
                nodeList.Remove(selectedNode);
                dialogueTree.nodes = nodeList.ToArray();
                selectedNode = null;
                EditorUtility.SetDirty(dialogueTree);
            }
        }
        
        EditorGUILayout.EndVertical();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(dialogueTree);
        }
    }
}