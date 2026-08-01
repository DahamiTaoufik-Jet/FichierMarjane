# Banque d'enigmes par spot

**Statut : 94 `StepData` dans `Assets/Scriptable Objects/Steps/Enigmes/`, toutes
enregistrees dans le `stepPool` du `RouteGeneratorConfig` (pool a 141 steps).**

La scene contient **380 `PlaceholderNode`** repartis sur 25 regions, dont les 9
regions jouets. Toutes les cibles utilisees ici existent donc reellement. Seule
exception connue dans le projet : `FinDeCouloir`, vise par `StepLookAt1`, qui n'a
aucun placeholder.

Les reponses qui doublonnaient une step deja existante (`GPU`, `SWITCH`, `3DS`,
`DONUT`, `BEURRE`, `REINE`, `CAFE`, `LAMPE`) ont ete retirees de la banque :
l'objectif etant justement de supprimer les repetitions, pas d'en ajouter.

Propositions de nouvelles `StepData` de type Puzzle (prefab `EnigmeObject`, `TextPuzzleStep`).

**Contraintes respectees** :
- Reponse en **un seul mot**, **sans accent**, **sans espace**.
  La comparaison est `OrdinalIgnoreCase` + `Trim()` (voir `TextPuzzleStep.cs:99`) :
  la casse est ignoree, mais un accent ou un espace fait echouer la validation.
- Questions sans accent, pour rester coherent avec le contenu existant.
- Une question = un angle different sur le meme objet, pour eviter la repetition
  actuelle (aujourd'hui un seul enonce par objet, toujours du type "devinette-definition").

---

## 1. Rayon Electro (regions avec placeholders en scene)

### ElectroKeyboard — le clavier
*18 placeholders (Keyboard0L..Keyboard5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Deux de mes touches portent un petit relief pour poser les index sans regarder. La premiere est F, quelle est la seconde ? | `J` |
| 2 | Sur un clavier francais, les six premieres lettres de la rangee du haut forment un nom. Lequel ? | `AZERTY` |
| 3 | Sur un clavier anglais, les six premieres lettres de la rangee du haut forment un autre nom. Lequel ? | `QWERTY` |
| 4 | Je suis la plus longue touche, celle qui separe les mots. | `ESPACE` |
| 5 | Format de clavier ampute de son pave numerique, abrege en trois lettres. | `TKL` |

### ElectroSouris — la souris
*18 placeholders (Souris0L..Souris5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Unite qui mesure ma sensibilite, en trois lettres. | `DPI` |
| 2 | Avant le capteur lumineux, je roulais sur une petite sphere. Comment l'appelait-on ? | `BILLE` |
| 3 | Technologie du capteur qui a remplace cette sphere. | `OPTIQUE` |
| 4 | Roue crantee entre mes deux boutons, pour faire defiler les pages. | `MOLETTE` |
| 5 | Surface souple sur laquelle je glisse. | `TAPIS` |

### ElectroEcran — l'ecran
*6 placeholders (Ecran0..Ecran5)*

| # | Question | Reponse |
|---|---|---|
| 1 | Plus petit point lumineux dont je suis compose. | `PIXEL` |
| 2 | Le nombre de ces points, en largeur et en hauteur, definit ma... | `RESOLUTION` |
| 3 | Unite dans laquelle se mesure ma frequence de rafraichissement. | `HERTZ` |
| 4 | Technologie a cristaux liquides, en trois lettres. | `LCD` |

### ElectroTv — le televiseur
*6 placeholders (Tv0..Tv5)*

| # | Question | Reponse |
|---|---|---|
| 1 | Boitier qui me commande depuis le canape. | `TELECOMMANDE` |
| 2 | Norme europeenne de prise audio-video a 21 broches. | `PERITEL` |
| 3 | Definition quatre fois superieure au Full HD, en deux caracteres. | `4K` |
| 4 | Chaque programme diffuse sur son numero : je suis une... | `CHAINE` |

### ElectroPc — la tour
*8 placeholders (Pc0..Pc7)*

| # | Question | Reponse |
|---|---|---|
| 1 | Mon cerveau, en trois lettres. | `CPU` |
| 2 | Grande carte sur laquelle tous les composants se branchent, en anglais. | `MOTHERBOARD` |
| 3 | Piece qui distribue le courant a tous mes composants. | `ALIMENTATION` |
| 4 | Piece qui tourne pour refroidir le processeur. | `VENTILATEUR` |

### ElectroGpu — la carte graphique
*18 placeholders (Gpu0L..Gpu5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Ma memoire video dediee, en quatre lettres. | `VRAM` |
| 2 | Technique de rendu qui simule le trajet reel des rayons de lumiere. | `RAYTRACING` |
| 3 | Nombre d'images calculees chaque seconde, abrege en trois lettres. | `FPS` |

*(`GPU` est deja la reponse de `StepEnigmeGpu`.)*

### ElectroLaptop — l'ordinateur portable
*18 placeholders (Laptop0L..Laptop5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Surface tactile rectangulaire qui me sert de souris. | `TOUCHPAD` |
| 2 | Ce qui me fait fonctionner loin de toute prise. | `BATTERIE` |
| 3 | Mecanisme qui me permet de me replier en deux. | `CHARNIERE` |
| 4 | Mot francais officiel pour dire laptop. | `PORTABLE` |

### ElectroSwitch — la console hybride
*15 placeholders (Switch1L..Switch5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Mes deux manettes amovibles qui se clipsent sur les cotes. | `JOYCON` |
| 2 | Console Nintendo de 2012 qui m'a precede, avec un ecran sur sa manette. | `WIIU` |
| 3 | Plombier moustachu, mascotte de la marque. | `MARIO` |

*(`SWITCH` est deja la reponse de `StepEnigmeSwitch`.)*

### Electro3DS — la console portable
*15 placeholders (3DS1L..3DS5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Effet de profondeur que j'affiche sans lunettes. | `RELIEF` |
| 2 | Console a clapet de 2004 dont je suis l'heritiere, en deux lettres. | `DS` |
| 3 | Petite tige avec laquelle on touche mon ecran du bas. | `STYLET` |

*(`3DS` est deja la reponse de `StepEnigme3DS`.)*

### ElectroGcController — la manette
*18 placeholders (GcController0L..GcController5R)*

| # | Question | Reponse |
|---|---|---|
| 1 | Ma croix directionnelle a quatre directions, en anglais. | `DPAD` |
| 2 | Petit levier que l'on incline avec le pouce. | `JOYSTICK` |
| 3 | Retour haptique qui me fait trembler dans les mains. | `VIBRATION` |
| 4 | Boutons places sous moi, actionnes par les index. | `GACHETTES` |

---

## 2. Rayon jouets et coin salon

Toutes ces regions possedent leurs `PlaceholderNode` en scene.

**Les enonces ci-dessous sont ancres sur les modeles 3D reels**, verifies dans la
scene. A retenir avant d'en ecrire d'autres :

| Region | Modele reel | Piege a eviter |
|---|---|---|
| `JouetSword` | Epee **en diamant Minecraft** : lame `#00BED4`/`#81DFEA`, manche `#785445`, contour `#191919` | Pas une epee de chevalier. Ni fourreau ni pommeau |
| `JouetBloc` | **Cube alphabet** 16 cm, faces crème `#FFE5C6`, lettres rouges `#CA403E` : `letterA`, `letterB`, `letterC`, chacune en double | Ce n'est **pas** un Lego : aucun tenon |
| `JouetBallon` | **Ballon de basket** : cuir `#BB8158`, rainures `#404040` | Pas un ballon generique |
| `JouetAvion` | `avion madera` : avion **en bois** massif, piece unique, brun `#4C2927` | Pas de cockpit ni de reacteur |
| `JouetVoiture` | 3 pieces : `Car_Dook`, `FrontWheels`, `BackWheels` | — |
| `JouetDeskToy` | **Pendule de Newton** : 5 billes d'acier sur cadre metallique | — |
| `JouetArrow` | Fleche : hampe brune `#5F322D`, pointe et empennage gris | — |

Note : les placeholders de `JouetVoiture` portent des `spotId` prefixes `Train0L`
… `Train5R`, identiques a ceux de `JouetTrain`. Sans consequence tant qu'aucune
step n'utilise le mode `Spot` sur ces deux rayons, mais a renommer.

### JouetVoiture

| # | Question | Reponse |
|---|---|---|
| 1 | Nombre de roues d'une voiture classique. | `QUATRE` |
| 2 | Vitre a l'avant qui protege du vent. | `PAREBRISE` |
| 3 | Ce qui me fait avancer, cache sous le capot. | `MOTEUR` |
| 4 | Je suis fait de trois pieces : la carrosserie, les roues avant et les roues... ? | `ARRIERE` |
| 5 | Enveloppe de caoutchouc noir qui entoure chaque roue. | `PNEU` |
| 6 | Barre qui relie deux roues et les fait tourner ensemble. | `ESSIEU` |

### JouetTrain

| # | Question | Reponse |
|---|---|---|
| 1 | Ce sur quoi je circule. | `RAILS` |
| 2 | Vehicule de tete qui tire tous les wagons. | `LOCOMOTIVE` |
| 3 | Lieu ou je m'arrete pour laisser monter les voyageurs. | `GARE` |
| 4 | Assemblage de wagons tires par une meme machine. | `CONVOI` |
| 5 | Piece mobile de la voie qui permet de changer de direction. | `AIGUILLAGE` |
| 6 | Homme qui pelletait le charbon dans la locomotive a vapeur. | `CHAUFFEUR` |

### JouetAvion

| # | Question | Reponse |
|---|---|---|
| 1 | Bande goudronnee d'ou je prends mon envol. | `PISTE` |
| 2 | Surface plate de chaque cote qui me porte dans les airs. | `AILE` |
| 3 | Celui qui me conduit depuis le cockpit. | `PILOTE` |
| 4 | Je suis taille dans un seul bloc d'un materiau brun et chaud. Lequel ? | `BOIS` |
| 5 | Piece qui tourne a l'avant pour me tirer dans l'air. | `HELICE` |
| 6 | Partie arriere verticale qui me permet de tourner. | `DERIVE` |

### JouetSword

| # | Question | Reponse |
|---|---|---|
| 1 | Ma partie longue et tranchante. | `LAME` |
| 2 | Piece qui protege la main entre la lame et la poignee. | `GARDE` |
| 3 | Ma lame turquoise n'est ni en fer ni en or. Quelle gemme la compose ? | `DIAMANT` |
| 4 | Pour me fabriquer il faut deux gemmes empilees et, juste en dessous, un objet en bois. Lequel ? | `BATON` |
| 5 | Un seul materiau me surpasse : on l'obtient en fusionnant mon diamant avec un lingot rapporte du Nether. | `NETHERITE` |

### JouetBloc

| # | Question | Reponse |
|---|---|---|
| 1 | Action de poser un bloc sur un autre, encore et encore. | `EMPILER` |
| 2 | Trois lettres sont gravees sur mes six faces, chacune en double. Cite la troisieme. | `C` |
| 3 | Combien de faces carrees compte le solide que je suis ? | `SIX` |
| 4 | Empiles les uns sur les autres, mes semblables forment une construction fragile. Comment l'appelle-t-on ? | `TOUR` |

### JouetBallon

| # | Question | Reponse |
|---|---|---|
| 1 | Forme geometrique parfaite qui est la mienne. | `SPHERE` |
| 2 | Ce qu'on souffle dedans pour me faire rebondir. | `AIR` |
| 3 | Cercle muni d'un filet dans lequel on me lance au basket. | `PANIER` |
| 4 | Combien de points vaut un tir reussi au-dela de la ligne exterieure ? | `TROIS` |
| 5 | Action de me faire rebondir au sol tout en avancant. | `DRIBBLE` |
| 6 | Plaque rectangulaire fixee derriere l'arceau. | `PANNEAU` |

### JouetArc

| # | Question | Reponse |
|---|---|---|
| 1 | Projectile que je propulse. | `FLECHE` |
| 2 | Fil tendu entre mes deux extremites. | `CORDE` |
| 3 | Panneau rond a anneaux colores que l'on vise. | `CIBLE` |
| 4 | Partie centrale de l'arc ou se pose la main. | `POIGNEE` |
| 5 | Zone centrale de la cible qui rapporte le maximum de points. | `MOUCHE` |
| 6 | Celui qui manie l'arc. | `ARCHER` |

### JouetArrow — la fleche

| # | Question | Reponse |
|---|---|---|
| 1 | Longue tige de bois qui forme mon corps. | `HAMPE` |
| 2 | Plumes fixees a mon extremite arriere pour stabiliser mon vol. | `EMPENNAGE` |
| 3 | Petite fente a l'arriere qui vient se loger sur la corde. | `ENCOCHE` |

### JouetDeskToy — le pendule de Newton

| # | Question | Reponse |
|---|---|---|
| 1 | Cinq billes d'acier suspendues : on en lache une, une seule repart de l'autre cote. Quel savant m'a donne son nom ? | `NEWTON` |
| 2 | Mouvement de va-et-vient regulier de mes billes. | `OSCILLATION` |
| 3 | Force qui finit toujours par immobiliser les billes. | `FROTTEMENT` |

### Sucrerie

| # | Question | Reponse |
|---|---|---|
| 1 | Couche sucree et brillante etalee sur le dessus d'un donut. | `GLACAGE` |
| 2 | Sucre chauffe jusqu'a devenir brun et coulant. | `CARAMEL` |

*(`DONUT` est deja la reponse de `StepEnigmeDonut`.)*

### Lait

| # | Question | Reponse |
|---|---|---|
| 1 | Procede de chauffage qui conserve le lait, du nom d'un savant francais. | `PASTEURISATION` |
| 2 | Partie grasse du lait qui remonte a la surface. | `CREME` |

*(`BEURRE` est deja la reponse de `StepEnigmeBeurre`.)*

### ChillZone — echiquier

| # | Question | Reponse |
|---|---|---|
| 1 | Seule piece de l'echiquier qui se deplace en L. | `CAVALIER` |
| 2 | Mot qui annonce la fin de la partie d'echecs. | `MAT` |

*(`REINE` est deja la reponse de `StepEnigmeChess`.)*

### ChillZone — mug et magazine

| # | Question | Reponse |
|---|---|---|
| 1 | Publication illustree qui parait chaque mois. | `MAGAZINE` |
| 2 | Partie courbe du mug par laquelle on le tient. | `ANSE` |

*(`CAFE` est deja la reponse de `StepEnigmeMugMagazine`.)*

### ChillZone — table et lampe

| # | Question | Reponse |
|---|---|---|
| 1 | Partie en verre qui s'allume a l'interieur de la lampe. | `AMPOULE` |
| 2 | Unite qui mesure la quantite de lumiere emise. | `LUMENS` |

*(`LAMPE` est deja la reponse de `StepEnigmeEmptyTable`.)*

---

## 3. Doublons a corriger dans le pool actuel

Ces steps existantes posent le probleme decrit : meme reponse, question quasi identique.

| Steps concernees | Reponse partagee | Action suggeree |
|---|---|---|
| `StepEnigme`, `StepEnigmeCacher 1`, `StepEnigmeSourisElectro` | `souris` | Garder `StepEnigmeSourisElectro`, reaffecter les deux autres depuis la banque ci-dessus |
| `StepEnigmeCacher`, `StepEnigmeKeyboard` | `clavier` | Garder `StepEnigmeKeyboard`, reaffecter `StepEnigmeCacher` |
