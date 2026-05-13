# Inventory/Data

Namespace : `EscapeGame.Inventory.Data`

ScriptableObjects des items stockables dans l'inventaire.

---

## Classes

### ItemData : RewardData (abstract)
Base abstraite des items stockables. Herite de `RewardData` (rewardName, description, icon) pour pouvoir etre directement reference comme recompense de fin de route.

### LetterItem : ItemData (SO)
Lettre obtenue a la fin d'une route. Reconstitue un mot de passe final.
- `char letter` : caractere effectif.
- Menu : `EscapeGame/Inventory/LetterItem`.

### BonusItem : ItemData
Base des bonus utilisables. Les implementations concretes heritent et surchargent `Execute`.
- `bool consumeOnUse = true` : si vrai, l'item est retire de l'inventaire apres utilisation.
- `virtual Execute(PlayerContext context)` : logique du bonus (a surcharger).
- `Use(PlayerContext context)` : verifie le contexte et appelle Execute.
- Les bonus concrets (PathFinderBonus, ChaudFroidBonus, DechiffreurBonus, ResolveurBonus) sont dans `Bonuses/Data/`.

### LetterAlphabet : ScriptableObject
Mapping char -> LetterItem pour le generateur de routes.
- `List<LetterItem> letters` : tous les LetterItem du jeu.
- `GetLetter(char c) -> LetterItem` : recherche insensible a la casse.
- Menu : `EscapeGame/Inventory/LetterAlphabet`.

---

## Dependances
- `EscapeGame.Routes.Data` (RewardData) — ItemData
- `EscapeGame.Core.Player` (PlayerContext) — BonusItem
