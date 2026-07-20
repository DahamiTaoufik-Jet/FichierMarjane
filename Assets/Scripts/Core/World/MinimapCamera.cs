using UnityEngine;
using EscapeGame.Core.Player;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Pilote la camera de la minimap : elle suit le joueur en XZ, reste au-dessus
    /// de lui et regarde vers le bas. En mode ROTATIF (player-up), elle tourne avec
    /// le yaw du joueur pour que celui-ci "regarde toujours vers le haut" sur la
    /// carte (le marqueur joueur, lui, reste fixe pointant vers le haut).
    ///
    /// La camera est placee SOUS le plafond (height) et regarde vers le bas : le
    /// toit/plafond, etant au-dessus d'elle, n'est pas rendu -> vue du dessus propre
    /// des rayons/murs/sol, sans avoir a trier les layers.
    ///
    /// Masque la minimap (et coupe son rendu) quand une UI plein ecran est ouverte
    /// (journal, pause...) via UIState.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        [Header("Suivi")]
        [Tooltip("Cible suivie (le joueur / PlayerArmature).")]
        public Transform target;

        [Tooltip("Hauteur de la camera au-dessus du joueur. Doit rester SOUS le plafond.")]
        public float height = 25f;

        [Tooltip("Si vrai : minimap rotative (player-up). Si faux : orientee nord (fixe).")]
        public bool rotateWithPlayer = true;

        [Header("Perf")]
        [Tooltip("Rend 1 frame sur N (2 = une frame sur deux). 1 = chaque frame.")]
        public int renderEveryNFrames = 2;

        [Header("Visibilite")]
        [Tooltip("Racine UI de la minimap : masquee quand une UI plein ecran est ouverte.")]
        public GameObject uiRoot;

        [Tooltip("Force la minimap visible meme si une UI est ouverte (ex. menu pause pour previsualiser les reglages).")]
        [HideInInspector] public bool forceVisible;

        private Camera cam;
        private int frame;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            bool uiOpen = UIState.IsAnyUIOpen && !forceVisible;

            if (uiRoot != null && uiRoot.activeSelf == uiOpen)
                uiRoot.SetActive(!uiOpen);

            if (target != null)
            {
                Vector3 p = target.position;
                transform.position = new Vector3(p.x, height, p.z);
                float yaw = rotateWithPlayer ? target.eulerAngles.y : 0f;
                transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            }

            if (cam == null) return;

            if (uiOpen)
            {
                cam.enabled = false; // pas de rendu quand la minimap est cachee
            }
            else if (renderEveryNFrames > 1)
            {
                frame++;
                cam.enabled = (frame % renderEveryNFrames) == 0;
            }
            else
            {
                cam.enabled = true;
            }
        }
    }
}
