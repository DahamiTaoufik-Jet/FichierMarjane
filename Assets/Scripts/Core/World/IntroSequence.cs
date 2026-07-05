using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using EscapeGame.Core.Player;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Cinematique d'introduction facon Mario 64 : au chargement de la scene, une
    /// CinemachineCamera d'intro (priorite superieure a la camera TPS) survole le
    /// marche le long d'une spline (CinemachineSplineDolly), puis rend la main au
    /// joueur. On ne fait pas la transition finale a la main : il suffit de baisser
    /// la priorite de l'intro et le CinemachineBrain fond automatiquement vers la
    /// camera TPS, qui "se pose" sur le joueur.
    ///
    /// Les inputs joueur sont bloques pendant toute la sequence via UIState
    /// (les controleurs de mouvement/visee font deja un early-return dessus).
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        [Header("Cameras")]
        [Tooltip("CinemachineCamera d'intro (priorite haute). Suit la spline via CinemachineSplineDolly.")]
        public CinemachineCamera introCamera;

        [Tooltip("Le CinemachineSplineDolly de la camera d'intro (sa CameraPosition est animee 0 -> 1).")]
        public CinemachineSplineDolly dolly;

        [Header("Timing")]
        [Tooltip("Duree du survol, en secondes.")]
        public float duration = 8f;

        [Tooltip("Avancement le long de la spline (0 -> 1). Une courbe ease in/out donne un mouvement doux.")]
        public AnimationCurve progress = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Priorites")]
        [Tooltip("Priorite de la camera d'intro pendant le survol (doit depasser la TPS = 10).")]
        public int introPriority = 20;

        [Tooltip("Priorite de la camera d'intro une fois la main rendue (0 = laisse passer la TPS).")]
        public int idlePriority = 0;

        [Header("Fin de sequence")]
        [Tooltip("Attente apres la bascule de priorite, pour laisser le CinemachineBrain finir son fondu vers la TPS.")]
        public float blendWait = 2f;

        [Header("Skip")]
        [Tooltip("Si vrai, une touche saute directement au fondu final vers le joueur.")]
        public bool skippable = true;

        public Key skipKey = Key.Enter;
        public Key skipKey2 = Key.Space;

        [Header("Regard (Look At)")]
        [Tooltip("Cible visee par la camera d'intro (un empty). La camera doit avoir LookAt = cette cible, " +
                 "et le CinemachineSplineDolly CameraRotation = FollowTargetNoRoll.")]
        public Transform lookTarget;

        [Tooltip("Si vrai : la camera regarde TOUJOURS le meme point (la position de lookTarget en scene, " +
                 "que tu peux deplacer librement) pendant toute la spline. Le script ne bouge pas la cible. " +
                 "Si faux : mode dynamique (regard perpendiculaire, mur dans le dos, retour vers le joueur a la fin).")]
        public bool lookAtFixedPoint = true;

        [Tooltip("Point regarde pendant le tour (centre du magasin) : la camera 'voit la scene'.")]
        public Vector3 tourLookPoint = new Vector3(18f, 5f, 18f);

        [Tooltip("Point regarde a la fin (le joueur), pour retrouver l'orientation de la vue de jeu.")]
        public Vector3 endLookPoint = new Vector3(31.8f, 1.3f, 34f);

        [Range(0f, 1f)]
        [Tooltip("Fraction du parcours (0-1) a partir de laquelle la camera commence a revenir vers le joueur.")]
        public float endLookStart = 0.75f;

        [Tooltip("Distance du point vise devant la camera, vers l'interieur (perpendiculaire a la trajectoire).")]
        public float lookAheadDistance = 20f;

        [Tooltip("Abaissement du point vise (leger plongee sur la scene).")]
        public float lookDownDrop = 4f;

        [Tooltip("Ne joue l'intro qu'une fois par session (evite de la rejouer sur un rechargement de scene). " +
                 "Mettre false pour toujours la jouer.")]
        public bool playOnce = true;

        private static bool alreadyPlayed;
        private bool running;

        private IEnumerator Start()
        {
            if (playOnce && alreadyPlayed)
            {
                if (introCamera != null) introCamera.Priority = idlePriority;
                if (introCamera != null) introCamera.gameObject.SetActive(false);
                yield break;
            }

            if (introCamera == null || dolly == null)
            {
                Debug.LogWarning("[IntroSequence] introCamera ou dolly non assigne : intro ignoree.");
                yield break;
            }

            alreadyPlayed = true;
            running = true;

            // Bloque le joueur (mouvement, camera, scan) pendant la cinematique.
            UIState.SetUIOpen();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            introCamera.gameObject.SetActive(true);
            introCamera.Priority = introPriority;
            dolly.CameraPosition = 0f;
            if (!lookAtFixedPoint && lookTarget != null) lookTarget.position = tourLookPoint;

            // Laisse une frame au Brain pour prendre la camera d'intro comme active.
            yield return null;

            float t = 0f;
            while (t < duration && running)
            {
                if (skippable && SkipPressed()) break;
                t += Time.deltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                float pos = Mathf.Clamp01(progress.Evaluate(k));
                dolly.CameraPosition = pos;
                UpdateLook(pos);
                yield return null;
            }
            dolly.CameraPosition = 1f;
            UpdateLook(1f);

            // Rend la main : le Brain fond de l'intro vers la TPS.
            introCamera.Priority = idlePriority;

            yield return new WaitForSeconds(blendWait);

            introCamera.gameObject.SetActive(false);
            UIState.SetUIClosed();
            running = false;
        }

        /// <summary>
        /// Oriente la camera dynamiquement : a chaque point de la spline elle
        /// regarde PERPENDICULAIREMENT a sa trajectoire, vers l'interieur du
        /// magasin. Comme le mur longe la trajectoire, il reste ainsi toujours
        /// dans le dos de la camera. Sur la derniere fraction du parcours, le
        /// regard revient vers le joueur pour retrouver l'orientation de la vue
        /// de jeu avant le fondu vers la TPS.
        /// </summary>
        private void UpdateLook(float pos)
        {
            if (lookTarget == null) return;

            // Mode simple : la cible ne bouge pas, la camera fixe toujours le meme
            // point (positionne en scene) jusqu'a la fin de la spline.
            if (lookAtFixedPoint) return;

            Vector3 lookPoint;
            var sc = dolly != null ? dolly.Spline : null;
            if (sc != null)
            {
                var p = sc.EvaluatePosition(pos);
                var tg = sc.EvaluateTangent(pos);
                Vector3 camPos = new Vector3(p.x, p.y, p.z);
                Vector3 tangent = new Vector3(tg.x, 0f, tg.z);

                // Perpendiculaire horizontale a la tangente, tournee vers l'interieur.
                Vector3 perp = new Vector3(-tangent.z, 0f, tangent.x);
                Vector3 toInterior = tourLookPoint - camPos; toInterior.y = 0f;
                if (Vector3.Dot(perp, toInterior) < 0f) perp = -perp;
                if (perp.sqrMagnitude > 0.0001f) perp.Normalize();

                lookPoint = camPos + perp * lookAheadDistance;
                lookPoint.y = camPos.y - lookDownDrop;
            }
            else
            {
                lookPoint = tourLookPoint;
            }

            float f = endLookStart >= 1f ? 0f : Mathf.Clamp01((pos - endLookStart) / (1f - endLookStart));
            f = Mathf.SmoothStep(0f, 1f, f);
            lookTarget.position = Vector3.Lerp(lookPoint, endLookPoint, f);
        }

        private bool SkipPressed()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb[skipKey].wasPressedThisFrame || kb[skipKey2].wasPressedThisFrame;
        }

        /// <summary>Reinitialise le "deja joue" (utile depuis un GameRestarter/menu).</summary>
        public static void ResetPlayed() { alreadyPlayed = false; }
    }
}
