# Routes/Data

Namespace : `EscapeGame.Routes.Data`

ScriptableObjects et structures de donnees pour le systeme de routes.

---

## Enums

### StepType
- `Clue` : indice (auto-resolu au scan).
- `Puzzle` : enigme exigeant une interaction specifique.

### PlacementMode
Contrainte de placement d'une etape :
- `Any` : accepte n'importe quel placeholder du bon type.
- `Region` : placeholder dont `regionId` correspond.
- `Spot` : placeholder dont `spotId` correspond exactement.

---

## Structs

### PlacementData [Serializable]
- `PlacementMode mode`
- `string regionId` (si mode == Region)
- `string spotId` (si mode == Spot)

---

## Classes

### ClueContent [Serializable]
Contenu multimedia d'un indice.
- `string text` : texte de l'indice (TextArea).
- `Sprite image` : visuel optionnel.
- `AudioClip audio` : son optionnel.
- `bool IsEmpty` : vrai si tout est vide/null.

### RewardData : ScriptableObject (abstract)
Base des recompenses.
- `string rewardName`
- `string description` (TextArea)
- `Sprite icon`

### StepData : ScriptableObject
Brique elementaire pour le generateur procedural. Aucune notion de role (entree/fin) ni de recompense.
- `string stepId` : identifiant unique.
- `StepType type` : Clue ou Puzzle.
- `ClueContent initialClue` : indice qui guide vers cette step.
- `PlacementData placement` : contrainte de placement.
- `string puzzleQuestion` : question (si TextPuzzleStep).
- `string puzzleAnswer` : reponse attendue (insensible a la casse, espaces trimes).
- `bool puzzleEncrypted` : si vrai, la question est chiffree.
- `string puzzleEncryptedQuestion` : version chiffree.
- `GameObject cluePrefab` : prefab si type == Clue.
- `GameObject puzzlePrefab` : prefab si type == Puzzle.
- `ResolvePrefab() -> GameObject` : retourne le prefab adapte au type.
- Menu : `EscapeGame/Routes/Step`.

### RouteGeneratorConfig : ScriptableObject
Configuration du generateur procedural.
- `List<StepData> stepPool` : pool de steps candidates.
- `List<string> passwordWords` : mots candidats pour le mot de passe.
- `int minPasswordLength = 4`
- `LetterAlphabet letterAlphabet` : mapping char -> LetterItem.
- `List<BonusPoolEntry> bonusPool` : pool de bonus avec maxCount par type.
- `int minRouteLength = 3`, `int maxRouteLength = 5`.
- `int maxStepUsage = 2` : max utilisation d'une meme StepData.
- `string regionFilter` : filtre optionnel par region.
- `int seed = 0` : seed (0 = aleatoire).
- Menu : `EscapeGame/Routes/RouteGeneratorConfig`.

### BonusPoolEntry [Serializable]
- `BonusItem bonus`
- `int maxCount = 1` : nombre max de distribution dans une generation.

---

## Dependances
- `EscapeGame.Inventory.Data` (BonusItem, LetterAlphabet) — RouteGeneratorConfig
