# Optimisations de performance — version simple

Résumé de ce qui a été fait pour réduire les triangles rendus (compteur **Tris/Verts** du Stats) et remonter le FPS.

## Le problème
La scène du magasin est très lourde : **~4,7 millions de triangles**, surtout à cause des **jouets et produits** posés sur les étagères (des milliers de petits objets très détaillés).

---

## 1. Minimap allégée
La minimap est une **2ᵉ caméra** qui filme le magasin d'en haut → elle refaisait le rendu de **toute** la scène à chaque frame.

- Les produits/jouets (~10 800 objets) sont mis sur un **layer `MinimapHidden`** que la caméra minimap **ignore**. Elle ne dessine plus que la **structure** (murs, sol, étagères, frigos).
- La minimap ne se rafraîchit plus qu'**1 frame sur 4**.
- La caméra du **jeu**, elle, montre toujours tout (aucun changement visuel en jeu).

## 2. Jouets simplifiés (décimation)
Les modèles de jouets étaient ultra-détaillés **mais répétés des milliers de fois** : 14 400 exemplaires pour seulement **363 modèles uniques**. Réduire les modèles uniques profite donc à tous les exemplaires d'un coup.

- Les **100 modèles les plus lourds** ont été réduits à **40 %** de leurs triangles.
- Outil : **UnityMeshSimplifier** (décimation en C# dans l'éditeur, **sans Blender**).
- Les modèles réduits sont stockés dans **`Assets/Art/Meshes_LOD/ToyLODs.asset`**.
- **Résultat : 4,7 M → 3,0 M triangles (−36 %)**, jouets **−51 %**.
- **Laissés en pleine résolution** : les **stands** (`MarketStand_1`) et les **souris** (remplacées séparément).

## 3. Matériaux murs / plafond
Deux matériaux simples et **mats** (`Mur_Simple`, `Plafond_Sombre`) pour éviter le rendu trop brillant / aveuglant des murs.

---

## Piste restante (pas encore faite)
- **Réactiver l'occlusion culling** (`useOcclusionCulling` sur la caméra) → ne plus dessiner ce qui est **caché derrière les murs/étagères**. Gros gain gratuit, **mais** ça avait été coupé pour régler le bug « des props qui disparaissent » → à refaire avec précaution (rebaker + tester).

## Revenir en arrière
Rien n'est détruit : les **meshes originaux existent toujours**, les jouets pointent juste vers les versions réduites. Pour annuler : réassigner les meshes d'origine, ou `git revert` du commit de décimation.
