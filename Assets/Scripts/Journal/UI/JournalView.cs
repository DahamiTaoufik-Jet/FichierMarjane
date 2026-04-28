using System.Collections.Generic;
using UnityEngine;
using EscapeGame.Journal.Runtime;
using EscapeGame.Routes.Runtime;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Vue principale du journal : conteneur de <see cref="RouteRowView"/>.
    /// S'abonne au <see cref="JournalManager"/> et instancie / met à jour les
    /// lignes de routes en conséquence.
    /// </summary>
    public class JournalView : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Source des données. Si null, recherche automatique de JournalManager.Instance au démarrage.")]
        public JournalManager journalManager;

        [Tooltip("Parent UI dans lequel les lignes de routes sont instanciées.")]
        public Transform rowsContainer;

        [Tooltip("Prefab d'une ligne de route (doit posséder un RouteRowView).")]
        public RouteRowView routeRowPrefab;

        private readonly Dictionary<RouteRuntime, RouteRowView> rows = new Dictionary<RouteRuntime, RouteRowView>();

        private void Start()
        {
            if (journalManager == null) journalManager = JournalManager.Instance;
            if (journalManager == null)
            {
                Debug.LogWarning("[JournalView] Aucun JournalManager trouvé.");
                return;
            }

            // Création initiale des lignes pour les routes déjà enregistrées
            for (int i = 0; i < journalManager.KnownRoutes.Count; i++)
                AddRow(journalManager.KnownRoutes[i]);
        }

        private void OnEnable()
        {
            if (journalManager != null)
            {
                journalManager.RouteAdded     += AddRow;
                journalManager.RouteUpdated   += RefreshRow;
                journalManager.RouteCompleted += RefreshRow;
            }
        }

        private void OnDisable()
        {
            if (journalManager != null)
            {
                journalManager.RouteAdded     -= AddRow;
                journalManager.RouteUpdated   -= RefreshRow;
                journalManager.RouteCompleted -= RefreshRow;
            }
        }

        private void AddRow(RouteRuntime route)
        {
            if (route == null || rows.ContainsKey(route)) return;
            if (routeRowPrefab == null || rowsContainer == null) return;

            var row = Instantiate(routeRowPrefab, rowsContainer);
            row.Bind(route);
            rows[route] = row;
        }

        private void RefreshRow(RouteRuntime route)
        {
            if (route == null) return;
            if (rows.TryGetValue(route, out var row)) row.Refresh();
        }
    }
}
