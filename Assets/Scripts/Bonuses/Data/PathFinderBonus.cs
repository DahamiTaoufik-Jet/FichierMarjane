using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Inventory.Data;

namespace EscapeGame.Bonuses.Data
{
    /// <summary>
    /// Bonus PathFinder : trace un chemin temporaire entre le joueur et la
    /// prochaine step non resolue la plus proche. Le chemin est materialise par
    /// une ligne discrete + un flux de particules vertes ("train") qui circule
    /// vers la cible. Le VFX est configure pour rester joli sans texture (trails
    /// + fondu d'alpha).
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeGame/Bonuses/PathFinder", fileName = "PathFinderBonus")]
    public class PathFinderBonus : BonusItem
    {
        [Header("PathFinder")]
        [Tooltip("Duree d'affichage du chemin en secondes.")]
        public float duration = 10f;

        [Tooltip("Hauteur au-dessus du sol pour le point de depart (joueur).")]
        public float playerHeightOffset = 1f;

        [Header("Ligne guide")]
        [Tooltip("Largeur de la ligne. Mettre 0 pour n'afficher que les particules.")]
        public float lineWidth = 0.04f;

        [Tooltip("Couleur de la ligne.")]
        public Color lineColor = new Color(0.25f, 1f, 0.45f, 0.5f);

        [Header("VFX particules (le 'train')")]
        [Tooltip("Couleur des particules / trails.")]
        public Color particleColor = new Color(0.25f, 1f, 0.45f, 1f);

        [Tooltip("Taille des particules.")]
        public float particleSize = 0.16f;

        [Tooltip("Vitesse de defilement des particules vers la cible.")]
        public float particleSpeed = 6f;

        [Tooltip("Nombre de particules emises par seconde.")]
        public float emissionRate = 70f;

        public override void Execute(PlayerContext context)
        {
            if (context == null) return;

            Transform player = context.GetPlayerTransform();
            var closest = BonusUtils.FindClosestUnresolvedStep(player.position);

            if (closest == null)
            {
                Debug.Log("[PathFinderBonus] Aucune step non resolue trouvee.");
                return;
            }

            Debug.Log($"[PathFinderBonus] Pointage vers '{closest.name}'.");
            SpawnPath(player, closest.transform);
        }

        private void SpawnPath(Transform player, Transform target)
        {
            var go = new GameObject("PathFinderLine");

            // --- Ligne guide ---
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.enabled = lineWidth > 0f;

            // --- VFX particules ---
            var vfx = BuildTrailVfx(go);

            var tracker = go.AddComponent<PathFinderLineTracker>();
            tracker.Init(player, target, playerHeightOffset, duration, vfx, particleSpeed);
        }

        /// <summary>
        /// Construit un ParticleSystem enfant : flux continu de particules vertes
        /// emises vers l'avant, avec trails (rubans) et fondu d'alpha pour un
        /// rendu propre sans aucune texture.
        /// </summary>
        private ParticleSystem BuildTrailVfx(GameObject parent)
        {
            var psGo = new GameObject("PathFinderVFX");
            psGo.transform.SetParent(parent.transform, false);

            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.8f;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = particleColor;
            main.gravityModifier = 0f;
            main.maxParticles = 2000;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 3f;
            shape.radius = 0.04f;

            // Fondu d'alpha sur la duree de vie (in/out doux).
            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colOverLife.color = grad;

            // Taille en comete (pousse puis retrecit).
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.2f);
            sizeCurve.AddKey(0.25f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Trails : c'est ce qui rend joli SANS texture (rubans verts).
            var trails = ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.35f);
            trails.minVertexDistance = 0.1f;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.6f);

            // Renderer : Sprites/Default (unlit, vertex color, alpha) -> pas besoin
            // de texture, la couleur verte + l'alpha suffisent.
            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.material = mat;
            psr.trailMaterial = mat;
            psr.alignment = ParticleSystemRenderSpace.View;

            ps.Play();
            return ps;
        }
    }
}
