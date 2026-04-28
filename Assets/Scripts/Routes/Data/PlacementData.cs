namespace EscapeGame.Routes.Data
{
    /// <summary>
    /// Décrit la contrainte de placement d'une étape. Utilisée par le générateur
    /// pour matcher la step avec un PlaceholderNode compatible dans la scène.
    /// </summary>
    [System.Serializable]
    public struct PlacementData
    {
        public PlacementMode mode;

        [UnityEngine.Tooltip("Identifiant de région (utilisé si mode == Region).")]
        public string regionId;

        [UnityEngine.Tooltip("Identifiant de spot exact (utilisé si mode == Spot).")]
        public string spotId;
    }
}
