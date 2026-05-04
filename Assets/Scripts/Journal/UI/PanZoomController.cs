using UnityEngine;
using UnityEngine.EventSystems;

namespace EscapeGame.Journal.UI
{
    /// <summary>
    /// Pan et zoom sur le WorldContainer du journal.
    /// A attacher sur le Viewport (zone de capture des inputs).
    /// </summary>
    public class PanZoomController : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [Header("Cible")]
        [Tooltip("Le RectTransform a deplacer/zoomer (WorldContainer).")]
        public RectTransform worldContainer;

        [Header("Zoom")]
        public float minScale = 0.35f;
        public float maxScale = 2.2f;
        public float scrollZoomFactor = 0.1f;
        public float buttonZoomIn = 1.25f;
        public float buttonZoomOut = 0.80f;

        private RectTransform viewportRect;

        private void Awake()
        {
            viewportRect = GetComponent<RectTransform>();
        }

        // ====================================================================
        // Drag → Pan
        // ====================================================================

        public void OnDrag(PointerEventData eventData)
        {
            if (worldContainer == null) return;
            worldContainer.anchoredPosition += eventData.delta / GetCanvasScale();
        }

        // ====================================================================
        // Scroll → Zoom centre sur curseur
        // ====================================================================

        public void OnScroll(PointerEventData eventData)
        {
            if (worldContainer == null) return;

            float scroll = eventData.scrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            float factor = 1f + scroll * scrollZoomFactor;
            float newScale = Mathf.Clamp(worldContainer.localScale.x * factor, minScale, maxScale);

            // Point du monde sous le curseur avant zoom
            Vector2 localCursor;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewportRect, eventData.position, eventData.pressEventCamera, out localCursor);

            Vector2 pivotOffset = localCursor - worldContainer.anchoredPosition;
            float scaleRatio = newScale / worldContainer.localScale.x;

            worldContainer.localScale = new Vector3(newScale, newScale, 1f);
            worldContainer.anchoredPosition = localCursor - pivotOffset * scaleRatio;
        }

        // ====================================================================
        // Boutons publics (wire dans l'Inspector)
        // ====================================================================

        public void ZoomIn()
        {
            if (worldContainer == null) return;
            float newScale = Mathf.Clamp(worldContainer.localScale.x * buttonZoomIn, minScale, maxScale);
            worldContainer.localScale = new Vector3(newScale, newScale, 1f);
        }

        public void ZoomOut()
        {
            if (worldContainer == null) return;
            float newScale = Mathf.Clamp(worldContainer.localScale.x * buttonZoomOut, minScale, maxScale);
            worldContainer.localScale = new Vector3(newScale, newScale, 1f);
        }

        public void ResetView()
        {
            if (worldContainer == null) return;
            worldContainer.anchoredPosition = Vector2.zero;
            worldContainer.localScale = Vector3.one;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private float GetCanvasScale()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) return canvas.scaleFactor;
            return 1f;
        }
    }
}
