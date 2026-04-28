using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Affiche une route sous forme d'une ligne de blocs cliquables
    /// (<see cref="StepBlockView"/>). Affiche également le nom de la route
    /// et applique l'état "doré" lorsque toutes les étapes sont résolues.
    /// </summary>
    public class RouteRowView : MonoBehaviour
    {
        [Header("Texte")]
        [Tooltip("Texte du nom de la route (UI Toolkit / TMP / UI standard).")]
        public Text routeNameLabel;

        [Header("Conteneur des blocs")]
        [Tooltip("Parent dans lequel les StepBlockView sont instanciés.")]
        public Transform blocksContainer;

        [Tooltip("Prefab d'un bloc d'étape (doit posséder un StepBlockView).")]
        public StepBlockView stepBlockPrefab;

        [Header("Mise en valeur 'route dorée'")]
        public Image rowBackground;
        public Color normalRowColor = new Color(1f, 1f, 1f, 0.05f);
        public Color goldenRowColor = new Color(1f, 0.85f, 0.2f, 0.20f);

        private RouteRuntime boundRoute;
        private readonly List<StepBlockView> spawnedBlocks = new List<StepBlockView>();

        public void Bind(RouteRuntime route)
        {
            boundRoute = route;
            BuildBlocks();
            Refresh();
        }

        public void Refresh()
        {
            if (boundRoute == null) return;

            if (routeNameLabel != null)
                routeNameLabel.text = string.IsNullOrEmpty(boundRoute.DisplayName)
                    ? boundRoute.RouteId
                    : boundRoute.DisplayName;

            for (int i = 0; i < spawnedBlocks.Count; i++)
                spawnedBlocks[i].Refresh();

            if (rowBackground != null)
                rowBackground.color = boundRoute.State == RouteState.Completed
                    ? goldenRowColor
                    : normalRowColor;
        }

        private void BuildBlocks()
        {
            ClearBlocks();
            if (boundRoute == null || stepBlockPrefab == null || blocksContainer == null) return;

            for (int i = 0; i < boundRoute.Steps.Count; i++)
            {
                var block = Instantiate(stepBlockPrefab, blocksContainer);
                block.Bind(boundRoute.Steps[i]);
                spawnedBlocks.Add(block);
            }
        }

        private void ClearBlocks()
        {
            for (int i = 0; i < spawnedBlocks.Count; i++)
                if (spawnedBlocks[i] != null) Destroy(spawnedBlocks[i].gameObject);
            spawnedBlocks.Clear();
        }
    }
}
