using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EscapeGame.Interactables.Puzzles;
using EscapeGame.Interactables.Clues;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Handles procedural placement of puzzles and their corresponding clues, linking them dynamically via GUIDs.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Available Prefabs")]
        public GameObject[] puzzlePrefabs; // e.g., TextPuzzle, AudioVisualPuzzle
        public GameObject[] cluePrefabs;

        private void Start()
        {
            // Execute generation on start
            GenerateProceduralPairs();
        }

        public void GenerateProceduralPairs()
        {
            // 2. Fetch all placeholder nodes currently active in the scene hierarchically
            PlaceholderNode[] allNodes = FindObjectsByType<PlaceholderNode>(FindObjectsSortMode.None);

            List<PlaceholderNode> puzzleNodes = allNodes.Where(n => n.nodeType == ProceduralNodeType.Puzzle).ToList();
            List<PlaceholderNode> clueNodes = allNodes.Where(n => n.nodeType == ProceduralNodeType.Clue).ToList();

            if (puzzleNodes.Count == 0 || clueNodes.Count == 0 || puzzlePrefabs.Length == 0 || cluePrefabs.Length == 0)
            {
                Debug.LogWarning("[LevelGenerator] Missing nodes or prefabs setup. Generation aborted.");
                return;
            }

            // 3. Selection randomly of one Puzzle placeholder node
            PlaceholderNode selectedPuzzleNode = puzzleNodes[UnityEngine.Random.Range(0, puzzleNodes.Count)];
            
            // 4. Filter clue nodes to strictly match the selected puzzle's geographical zone ID
            List<PlaceholderNode> validClueNodesInZone = clueNodes.Where(n => n.zoneID == selectedPuzzleNode.zoneID).ToList();

            if (validClueNodesInZone.Count == 0)
            {
                Debug.LogError($"[LevelGenerator] No corresponding Clue node found inside boundary: {selectedPuzzleNode.zoneID}");
                return;
            }

            // Pick a clue location in the same zone
            PlaceholderNode selectedClueNode = validClueNodesInZone[UnityEngine.Random.Range(0, validClueNodesInZone.Count)];

            // Choose random prefabs to spawn
            GameObject chosenPuzzlePrefab = puzzlePrefabs[UnityEngine.Random.Range(0, puzzlePrefabs.Length)];
            GameObject chosenCluePrefab = cluePrefabs[UnityEngine.Random.Range(0, cluePrefabs.Length)];

            // 5. Instantiation of both elements
            GameObject spawnedPuzzle = Instantiate(chosenPuzzlePrefab, selectedPuzzleNode.transform.position, selectedPuzzleNode.transform.rotation);
            GameObject spawnedClue = Instantiate(chosenCluePrefab, selectedClueNode.transform.position, selectedClueNode.transform.rotation);

            // 6. Dynamic Unique ID Generation (Guid) to Link them together regardless of script config
            string relationGuid = Guid.NewGuid().ToString();

            // Inject the ID inside the logically correlated scripts
            PuzzleBase puzzleScript = spawnedPuzzle.GetComponent<PuzzleBase>();
            if (puzzleScript != null)
            {
                puzzleScript.pairID = relationGuid;
            }

            CluePanel clueScript = spawnedClue.GetComponent<CluePanel>();
            if (clueScript != null)
            {
                clueScript.pairID = relationGuid;
            }

            Debug.Log($"[LevelGenerator] Setup Complete. Linked Puzzle to Clue via Guid: {relationGuid} in {selectedPuzzleNode.zoneID}.");
            
            // Optional cleanup of placeholders
            // Destroy(selectedPuzzleNode.gameObject);
            // Destroy(selectedClueNode.gameObject);
        }
    }
}
