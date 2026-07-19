using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SowurShield.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue Tree", menuName = "Dialogue/Dialogue Tree")]
    public class DialogueTree : ScriptableObject
    {
        [Header("Tree Information")]
        public string conversationId;
        [TextArea(2, 4)]
        public string conversationDescription;

        [Header("Tree Settings")]
        public string startNodeId = "start";
        public bool isRepeatable = true;
        public bool saveProgressOnExit = true;

        [Header("Nodes")]
        public DialogueNode[] nodes = new DialogueNode[0];

        [Header("Metadata")]
        public string[] tags = new string[0]; // For categorizing conversations
        public int priority = 0; // Higher priority conversations override lower ones

        // Cache for quick node lookup
        private Dictionary<string, DialogueNode> nodeCache;

        /// <summary>
        /// Gets a node by its ID
        /// </summary>
        public DialogueNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return null;

            // Build cache if needed
            if (nodeCache == null)
                BuildNodeCache();

            nodeCache.TryGetValue(nodeId, out DialogueNode node);
            return node;
        }

        /// <summary>
        /// Gets the starting node for this conversation
        /// </summary>
        public DialogueNode GetStartNode()
        {
            return GetNode(startNodeId);
        }

        /// <summary>
        /// Finds the next available node based on conditions
        /// </summary>
        public DialogueNode GetNextAvailableNode(string fromNodeId)
        {
            var currentNode = GetNode(fromNodeId);
            if (currentNode == null)
                return null;

            // If current node has choices, we shouldn't auto-advance
            if (currentNode.HasChoices())
                return null;

            // Get the next node
            if (!string.IsNullOrEmpty(currentNode.nextNodeId))
            {
                var nextNode = GetNode(currentNode.nextNodeId);
                if (nextNode != null && nextNode.ShouldDisplay())
                    return nextNode;
            }

            return null;
        }

        /// <summary>
        /// Validates the entire dialogue tree
        /// </summary>
        public bool ValidateTree()
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(conversationId))
            {
                isValid = false;
            }

            if (nodes == null || nodes.Length == 0)
            {
                return false;
            }

            // Check for duplicate node IDs
            var nodeIds = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.nodeId))
                {
                    isValid = false;
                    continue;
                }

                if (nodeIds.Contains(node.nodeId))
                {
                    isValid = false;
                }
                else
                {
                    nodeIds.Add(node.nodeId);
                }

                // Validate individual node
                if (!node.IsValid())
                    isValid = false;
            }

            // Check if start node exists
            var startNode = GetNode(startNodeId);
            if (startNode == null)
            {
                isValid = false;
            }

            // Check for orphaned references
            foreach (var node in nodes)
            {
                // Check nextNodeId references
                if (!string.IsNullOrEmpty(node.nextNodeId) && GetNode(node.nextNodeId) == null)
                {
                }

                // Check choice references
                if (node.choices != null)
                {
                    foreach (var choice in node.choices)
                    {
                        if (!choice.isExitChoice && !string.IsNullOrEmpty(choice.nextNodeId) && GetNode(choice.nextNodeId) == null)
                        {
                        }
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Gets all nodes that can be reached from the start node
        /// </summary>
        public DialogueNode[] GetReachableNodes()
        {
            var reachable = new HashSet<string>();
            var toProcess = new Queue<string>();

            // Start with the start node
            toProcess.Enqueue(startNodeId);

            while (toProcess.Count > 0)
            {
                string currentId = toProcess.Dequeue();
                if (reachable.Contains(currentId))
                    continue;

                reachable.Add(currentId);
                var node = GetNode(currentId);
                if (node == null)
                    continue;

                // Add next node
                if (!string.IsNullOrEmpty(node.nextNodeId))
                    toProcess.Enqueue(node.nextNodeId);

                // Add choice targets
                if (node.choices != null)
                {
                    foreach (var choice in node.choices)
                    {
                        if (!choice.isExitChoice && !string.IsNullOrEmpty(choice.nextNodeId))
                            toProcess.Enqueue(choice.nextNodeId);
                    }
                }
            }

            return nodes.Where(n => reachable.Contains(n.nodeId)).ToArray();
        }

        /// <summary>
        /// Creates a simple linear dialogue tree from text array (for migration)
        /// </summary>
        public static DialogueTree CreateFromLinearDialogue(string[] dialogueTexts, string conversationId = null)
        {
            var tree = CreateInstance<DialogueTree>();
            tree.conversationId = conversationId ?? "converted_dialogue";
            tree.conversationDescription = "Converted from linear dialogue";

            var nodeList = new List<DialogueNode>();

            for (int i = 0; i < dialogueTexts.Length; i++)
            {
                var node = new DialogueNode();
                node.nodeId = i == 0 ? "start" : $"node_{i}";
                node.dialogueText = new UnityEngine.Localization.LocalizedString(); // legacy helper: text needs to be wired to a table entry manually after conversion
                node.speakerName = ""; // Will need to be set manually
                node.nodeType = NodeType.Dialogue;

                // Set next node (except for last node)
                if (i < dialogueTexts.Length - 1)
                    node.nextNodeId = $"node_{i + 1}";

                nodeList.Add(node);
            }

            tree.nodes = nodeList.ToArray();
            return tree;
        }

        private void BuildNodeCache()
        {
            nodeCache = new Dictionary<string, DialogueNode>();

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.nodeId))
                    {
                        nodeCache[node.nodeId] = node;
                    }
                }
            }
        }

        private void OnValidate()
        {
            // Clear cache when data changes in editor
            nodeCache = null;
        }
    }
} // namespace SowurShield.Dialogue
