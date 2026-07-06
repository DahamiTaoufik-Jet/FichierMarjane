using UnityEngine;
using EscapeGame.Core.Player;
using EscapeGame.Routes.Services;

namespace EscapeGame.Core.World
{
    /// <summary>
    /// Musique de fond du jeu. Demarre au moment PRECIS ou le joueur peut bouger
    /// (des que plus aucune UI n'est ouverte via UIState ; dans SampleScene cela
    /// correspond a la fin de la cinematique d'intro). La musique demarre une
    /// seule fois puis boucle.
    ///
    /// A l'obtention d'une lettre ou d'un bonus, <see cref="DuckForClip"/> met la
    /// musique EN PAUSE le temps du clip de decouverte, puis la relance avec un
    /// FONDU vers son volume initial a la fin de ce clip.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        public static BackgroundMusic Instance { get; private set; }

        [Tooltip("Source audio de la musique de fond (loop, 2D). Si null, prise sur ce GameObject.")]
        public AudioSource source;

        [Tooltip("Si vrai, attend que le joueur ait le controle (aucune UI ouverte) pour demarrer. " +
                 "Si faux, demarre immediatement.")]
        public bool waitForPlayerControl = true;

        [Tooltip("Duree du fondu de reprise apres le clip de decouverte (secondes).")]
        public float resumeFadeDuration = 1.5f;

        [Tooltip("Duree du fondu de sortie au deverrouillage des coffres (secondes).")]
        public float stopFadeDuration = 2f;

        private float initialVolume = 1f;
        private bool started;
        private bool stopped;   // arret definitif (deverrouillage des coffres)
        private bool stopping;  // fondu de sortie en cours
        private float stopTimer;
        private float stopFromVolume;

        // Etat du ducking (pause pendant la decouverte + fondu de reprise).
        private bool ducking;      // en pause le temps du clip
        private bool fading;       // fondu de reprise en cours
        private float duckClipLength;
        private float duckTimer;
        private float fadeTimer;

        private void Awake()
        {
            Instance = this;
            if (source == null) source = GetComponent<AudioSource>();
            if (source != null)
            {
                source.loop = true;
                source.playOnAwake = false;
                initialVolume = source.volume;
            }
        }

        private void OnEnable()
        {
            PasswordManager.ChestPhaseStarted += StopMusic;
        }

        private void OnDisable()
        {
            PasswordManager.ChestPhaseStarted -= StopMusic;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (stopped) return;

            // Fondu de sortie (deverrouillage des coffres) : prioritaire.
            if (stopping)
            {
                if (source == null) { stopped = true; return; }
                stopTimer += Time.deltaTime;
                float ks = stopFadeDuration > 0f ? Mathf.Clamp01(stopTimer / stopFadeDuration) : 1f;
                source.volume = Mathf.Lerp(stopFromVolume, 0f, ks);
                if (ks >= 1f)
                {
                    source.Stop();
                    source.volume = initialVolume;
                    stopping = false;
                    stopped = true;
                }
                return;
            }

            // Demarrage : attend le controle joueur.
            if (!started)
            {
                if (waitForPlayerControl && UIState.IsAnyUIOpen) return;
                started = true;
                if (source != null && !source.isPlaying) source.Play();
                return;
            }

            if (source == null) return;

            if (ducking)
            {
                // Reste en pause pendant toute la duree du clip de decouverte.
                duckTimer += Time.deltaTime;
                if (duckTimer >= duckClipLength)
                {
                    ducking = false;
                    fading = true;
                    fadeTimer = 0f;
                    source.volume = 0f;
                    if (!source.isPlaying) source.UnPause();
                }
            }
            else if (fading)
            {
                // Fondu de reprise vers le volume initial.
                fadeTimer += Time.deltaTime;
                float k = resumeFadeDuration > 0f ? Mathf.Clamp01(fadeTimer / resumeFadeDuration) : 1f;
                source.volume = Mathf.Lerp(0f, initialVolume, k);
                if (k >= 1f)
                {
                    source.volume = initialVolume;
                    fading = false;
                }
            }
        }

        /// <summary>
        /// Met la musique en pause le temps du clip de decouverte fourni, puis
        /// programme une reprise en fondu vers le volume initial a la fin du clip.
        /// Appele a l'obtention d'une lettre / d'un bonus.
        /// </summary>
        public void DuckForClip(AudioClip clip)
        {
            if (source == null || !started || stopped || stopping) return;

            duckClipLength = clip != null ? clip.length : 0f;
            duckTimer = 0f;
            ducking = true;
            fading = false;

            source.volume = initialVolume; // sera remis a 0 puis fondu a la reprise
            if (source.isPlaying) source.Pause();
        }

        /// <summary>
        /// Arrete la musique de fond avec un FONDU de sortie (appele au
        /// deverrouillage des coffres). Elle ne redemarre plus ensuite.
        /// </summary>
        public void StopMusic()
        {
            if (stopped || stopping) return;
            ducking = false;
            fading = false;

            if (source == null || !source.isPlaying)
            {
                // Rien a fondre (deja a l'arret ou en pause) -> arret direct.
                if (source != null) source.Stop();
                stopped = true;
                return;
            }

            stopFromVolume = source.volume;
            stopTimer = 0f;
            stopping = true;
        }
    }
}
