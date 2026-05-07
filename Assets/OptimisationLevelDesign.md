# Optimisation Level Design — Escape Game Unity 6 URP

Checklist des optimisations a appliquer sur la scene quand les props et decors seront en place.
Ordre de priorite : du plus simple/impactant au plus avance.

---

## 1. Static Batching (2 min)

Tous les props decoratifs non interactibles doivent etre marques **Static** dans l'Inspector (checkbox en haut a droite du GameObject).

- Unity combine les meshes statiques partageant le meme materiau en un seul draw call
- **Ne PAS cocher Static** sur : le joueur, les steps (enigmes/indices), les objets animes
- Regle : moins de materiaux differents = moins de draw calls. Reutiliser les memes materiaux autant que possible

---

## 2. Bake des lumieres (15 min)

Les lumieres statiques doivent etre bakees pour ne couter zero au runtime.

1. Marquer les lumieres fixes en **Baked** ou **Mixed** (Inspector → Light → Mode)
2. Marquer les objets recevant la lumiere en **Static** (Contribute GI)
3. Window → Rendering → Lighting → Generate Lighting
4. Garder en **Realtime** uniquement : lampe torche du joueur, effets dynamiques
5. Placer des **Light Probes** dans les zones de passage du joueur pour qu'il recoive la lumiere ambiante bakee

---

## 3. GPU Instancing (5 min)

Pour les objets repetes (chaises, livres, bouteilles, lampes, cadres...) :

1. Selectionner le materiau partage
2. Inspector → cocher **Enable GPU Instancing**
3. 50 objets identiques = 1 seul draw call au lieu de 50

Fonctionne uniquement si les objets partagent le meme mesh + meme materiau.

---

## 4. Occlusion Culling (10 min)

Empeche Unity de rendre ce qui est cache derriere un mur ou un obstacle.

1. Marquer les props en **Occludee Static** (se font cacher)
2. Marquer les gros murs/sols/plafonds en **Occluder Static** (cachent les autres)
3. Window → Rendering → Occlusion Culling → Bake
4. Tres efficace en interieur (les pieces d'une escape room se cachent mutuellement)

---

## 5. Textures

### Atlas
- Combiner les textures des props en une seule grande texture (Sprite Atlas ou texture atlas Blender)
- 1 atlas = 1 materiau = 1 draw call pour tous les props qui l'utilisent

### Compression
- **ASTC** sur Mac M1 / mobile
- **BC7** sur PC
- Ne jamais laisser de textures non compressees

### Resolution
- Objet qu'on voit de pres (table, porte) : 1024 ou 2048
- Petit objet (bibelot, livre, bouteille) : 256 ou 512
- Objet lointain ou rarement vu de pres : 256

### Mipmaps
- Toujours actives (Unity le fait par defaut). Reduit le cout des textures vues de loin.

---

## 6. Meshes

### Faces cachees
Supprimer dans Blender avant import :
- Le dessous d'un meuble pose au sol
- L'arriere d'une armoire collee au mur
- L'interieur d'un objet ferme qu'on ne voit jamais

### Combiner les petits meshes
Des objets proches et statiques (ex : une etagere + ses bibelots) peuvent etre combines en un seul mesh dans Blender. Moins d'objets = moins de draw calls.

### Budget triangles
Pour une escape room en interieur, viser :
- Scene totale : 200k - 500k triangles
- Un meuble : 500 - 3000 tris
- Un petit objet : 50 - 500 tris
- Murs/sols/plafonds : aussi peu que possible (geometrie simple)

---

## 7. URP (parametres du pipeline)

Verifier dans le URP Asset (Project Settings → Graphics) :

- **Rendering Path** : Forward (par defaut, adapte a peu de lumieres)
- **Depth Priming** : activer sur M1 (bon gain)
- **HDR** : desactiver si pas besoin de bloom/tonemapping
- **MSAA** : 2x suffit (ou desactiver si post-process anti-aliasing actif)
- **Shadow Resolution** : 1024 pour les ombres bakees, 2048 max pour la lumiere realtime du joueur
- **Shadow Distance** : reduire au minimum necessaire (10-15m en interieur)

---

## Resume rapide

| Action | Temps | Impact |
|---|---|---|
| Tout cocher Static | 2 min | Tres fort |
| Bake lumieres | 15 min | Tres fort |
| GPU Instancing sur materiaux repetes | 5 min | Fort |
| Occlusion Culling bake | 10 min | Fort |
| Texture atlas + compression | 30 min | Fort |
| Supprimer faces cachees (Blender) | 1h+ | Moyen |
| Combiner meshes proches (Blender) | 1h+ | Moyen |
