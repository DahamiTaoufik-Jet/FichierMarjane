using System;
using UnityEngine;

namespace EscapeGame.Routes.Services
{
    public class PasswordManager : MonoBehaviour
    {
        public static PasswordManager Instance { get; private set; }

        [Header("Config")]
        [Tooltip("Nombre d'essais globaux avant de perdre.")]
        public int maxAttempts = 3;

        [Header("Debug")]
        [Tooltip("Si renseigne, ce mot de passe est utilise au lieu de celui du generateur.")]
        public string debugPassword;

        private string password;
        private int attemptsUsed;
        private bool[] solvedPositions;
        // Positions deja "annoncees" par l'effet de revelation de lettre, pour
        // mapper les lettres repetees a des positions successives dans l'ordre
        // de leur obtention.
        private bool[] announcedPositions;

        public string Password => password;
        public int AttemptsRemaining => maxAttempts - attemptsUsed;
        public bool IsGameLost => attemptsUsed >= maxAttempts;
        public bool IsAllSolved
        {
            get
            {
                if (solvedPositions == null) return false;
                for (int i = 0; i < solvedPositions.Length; i++)
                    if (!solvedPositions[i]) return false;
                return true;
            }
        }

        public static event Action<int> AttemptFailed;
        public static event Action<int, char> PositionSolved;
        public static event Action AllSolved;
        public static event Action GameLost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(debugPassword) && string.IsNullOrEmpty(password))
            {
                SetPassword(debugPassword);
                Debug.Log($"[PasswordManager] Debug password force : {debugPassword}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetPassword(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            if (!string.IsNullOrEmpty(debugPassword) && !string.IsNullOrEmpty(password))
            {
                Debug.Log($"[PasswordManager] SetPassword ignore (debugPassword actif : {password})");
                return;
            }
            password = word.ToUpper();
            solvedPositions = new bool[password.Length];
            announcedPositions = new bool[password.Length];
            attemptsUsed = 0;
            Debug.Log($"[PasswordManager] Mot de passe configure : {password} ({password.Length} lettres)");
        }

        public char GetExpectedLetter(int position)
        {
            if (string.IsNullOrEmpty(password)) return '\0';
            if (position < 0 || position >= password.Length) return '\0';
            return password[position];
        }

        /// <summary>
        /// Renvoie l'index (base 0) de la lettre donnee dans le mot de passe,
        /// pour l'effet de revelation "a memoriser". Pour les lettres repetees,
        /// renvoie a chaque appel la prochaine position non encore annoncee, dans
        /// l'ordre d'obtention. Renvoie -1 si la lettre n'est pas dans le mot ou
        /// si aucun mot de passe n'est configure.
        /// </summary>
        public int GetPositionForLetter(char letter)
        {
            if (string.IsNullOrEmpty(password)) return -1;
            char upper = char.ToUpper(letter);

            if (announcedPositions == null || announcedPositions.Length != password.Length)
                announcedPositions = new bool[password.Length];

            // Premiere position correspondante pas encore annoncee.
            for (int i = 0; i < password.Length; i++)
            {
                if (!announcedPositions[i] && password[i] == upper)
                {
                    announcedPositions[i] = true;
                    return i;
                }
            }

            // Repli : toutes deja annoncees -> renvoyer la premiere correspondance.
            for (int i = 0; i < password.Length; i++)
                if (password[i] == upper) return i;

            return -1;
        }

        public bool IsPositionSolved(int position)
        {
            if (solvedPositions == null) return false;
            if (position < 0 || position >= solvedPositions.Length) return false;
            return solvedPositions[position];
        }

        public enum TryResult { Correct, Wrong, AlreadySolved, Lost, InvalidPosition }

        public TryResult TryLetter(int position, char letter)
        {
            if (string.IsNullOrEmpty(password))
                return TryResult.InvalidPosition;
            if (position < 0 || position >= password.Length)
                return TryResult.InvalidPosition;
            if (solvedPositions[position])
                return TryResult.AlreadySolved;
            if (IsGameLost)
                return TryResult.Lost;

            char expected = password[position];
            char upper = char.ToUpper(letter);

            if (upper == expected)
            {
                solvedPositions[position] = true;
                Debug.Log($"[PasswordManager] Position {position + 1} correcte : '{upper}'");
                PositionSolved?.Invoke(position, upper);

                if (IsAllSolved)
                {
                    Debug.Log("[PasswordManager] Mot de passe complet !");
                    AllSolved?.Invoke();
                }
                return TryResult.Correct;
            }

            attemptsUsed++;
            Debug.Log($"[PasswordManager] Mauvaise lettre '{upper}' en position {position + 1}. " +
                      $"Essais restants : {AttemptsRemaining}");
            AttemptFailed?.Invoke(AttemptsRemaining);

            if (IsGameLost)
            {
                Debug.Log("[PasswordManager] Tous les essais epuises. Perdu.");
                GameLost?.Invoke();
                return TryResult.Lost;
            }

            return TryResult.Wrong;
        }
    }
}
