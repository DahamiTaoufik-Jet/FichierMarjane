# Journal UI — Implementation Guide for Unity Canvas Legacy

## Context

This document describes the UI system for a puzzle game's level selection map (called "Journal").
The map displays procedurally generated routes and stages, connected by lines, with pan/zoom navigation.
The UI is built with **Unity Canvas Legacy (uGUI)**, NOT UI Toolkit.

---

## Scene Hierarchy (already built)

```
Canvas
└── JournalView
    ├── HatchBorder                  ← Image, tiled hatch pattern, full stretch
    ├── BackButton                   ← Button (TMP), bottom-right of JournalView
    │   └── Text (TMP)
    ├── Scroll View                  ← ScrollRect DISABLED
    │   └── Viewport                 ← Rect Mask 2D, inset 52px on all sides
    │       ├── WorldContainer       ← Empty RectTransform, Anchor Top-Left, Pivot (0,1)
    │       ├── ProgressBar          ← Slider, Interactable OFF, Handle removed
    │       └── ZoomBtnGrp           ← Vertical Layout Group, top-right
    │           ├── ZoomIn           ← Button "+"
    │           ├── ZoomOut          ← Button "−"
    │           └── ZoomReset        ← Button "↺"
    └── (StageModal — NOT YET, reserved for later)
```

---

## Prefabs (already built, located in Assets/Prefabs)

### `StageNode` (Button)
```
StageNode                    58 × 58 px, Button component
├── Background               Image — color changed by code
├── NumberText               TextMeshPro — 11px, bold, top-center
└── IconGroup                RectTransform, bottom-center
    ├── DoubleCheckIcon      Image — shown when Completed
    ├── LockOpenIcon         Image — shown when Current
    └── LockClosedIcon       Image — shown when Locked
```
- Button Transition: **None** (code handles all visual states)
- Raycast Target: ON (needed for click)

### `ConnectorLine` (Image)
```
ConnectorLine                Image component only, no children
                             Width: 100 (overridden by code)
                             Height: 3
                             Pivot: (0, 0.5)
                             Raycast Target: OFF
```

---

## Data Structures

The procedural generator provides a `RouteGraph`. These classes must exist:

```csharp
public enum StageState { Completed, Current, Locked }

[System.Serializable]
public class StageData
{
    public string id;
    public string title;
    public string number;       // display label e.g. "01", "02"
    public StageState state;    // set by builder based on progressIndex
}

[System.Serializable]
public class RouteData
{
    public string id;
    public string label;             // e.g. "Route A"
    public List<StageData> stages;
    public int progressIndex;        // index of the ONLY unlocked (Current) stage
                                     // stages[i < progressIndex]  = Completed
                                     // stages[progressIndex]      = Current
                                     // stages[i > progressIndex]  = Locked
}

[System.Serializable]
public class RouteGraph
{
    public List<RouteData> routes;
}
```

---

## Scripts to Implement

### 1. `PanZoomController.cs`
**Attach to: `Viewport`**  
**Ref needed: `WorldContainer` (RectTransform)**

Responsibilities:
- **Mouse drag** → pan WorldContainer via `anchoredPosition`
- **Scroll wheel** → zoom via `localScale`, centered on cursor position
- **Touch drag** (1 finger) → pan
- **Pinch** (2 fingers) → zoom
- **ZoomIn / ZoomOut / Reset buttons** → call public methods
- Clamp pan so the world stays reachable (don't pan past world bounds)

Key values:
```csharp
float minScale = 0.35f;
float maxScale = 2.2f;
// Zoom centered on cursor: keep world point under cursor fixed during scale
// newTx = cursorX - worldPointX * newScale
// newTy = cursorY - worldPointY * newScale
```

Public methods needed:
```csharp
public void ZoomIn()    // scale *= 1.25, clamp
public void ZoomOut()   // scale *= 0.80, clamp
public void ResetView() // anchoredPosition = (0,0), localScale = (1,1,1)
```

Wire ZoomIn/ZoomOut/Reset buttons via Inspector OnClick.

---

### 2. `LevelMapBuilder.cs`
**Attach to: `JournalView` (or a manager object)**  
**Called by: the procedural generator**

Responsibilities:
- Receive a `RouteGraph` from the procedural generator
- Clear WorldContainer children on rebuild
- For each route: instantiate a RouteContainer (empty GameObject)
- For each stage in route: instantiate `StageNode` prefab, position it
- For each consecutive stage pair: instantiate `ConnectorLine` prefab, position + rotate it
- Update `ProgressBar.value`

Layout constants:
```csharp
float hStep    = 130f;   // horizontal distance between stage centers
float zigAmp   = 28f;    // vertical zigzag amplitude (alternates +/-)
float routeGap = 140f;   // vertical distance between route baselines
float startX   = 80f;    // x position of first stage in each route
```

Stage position formula:
```csharp
float x = startX + stageIndex * hStep;
float y = -(routeIndex * routeGap) + (stageIndex % 2 == 0 ? zigAmp : -zigAmp);
Vector2 pos = new Vector2(x, y);
```

ConnectorLine positioning:
```csharp
// Place at midpoint, rotate toward next stage
Vector2 dir = posB - posA;
float dist = dir.magnitude;
line.sizeDelta = new Vector2(dist, isActive ? 3f : 2f);
line.anchoredPosition = posA;   // pivot is (0, 0.5) so starts at posA
float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
line.localEulerAngles = new Vector3(0, 0, angle);
line.GetComponent<Image>().color = isActive
    ? new Color(0.12f, 0.12f, 0.12f)
    : new Color(0.80f, 0.80f, 0.80f);
```

A connector is **active** (dark, solid) if `stages[i].state == Completed`.
A connector is **inactive** (grey) if `stages[i].state == Current` or `Locked`.

Public interface:
```csharp
public void Build(RouteGraph graph)        // called by generator on load
public void AdvanceTo(string routeId, int targetIndex)  // called on stage click
```

Inspector references needed:
```csharp
public RectTransform worldContainer;
public GameObject stageNodePrefab;
public GameObject connectorLinePrefab;
public Slider progressBar;
```

---

### 3. `StageNode.cs`
**Attach to: `StageNode` prefab**

Responsibilities:
- Receive stage data and display correct visual state
- Show/hide the correct icon (DoubleCheck / LockOpen / LockClosed)
- Handle click → notify LevelMapBuilder to advance progress

```csharp
public void Init(StageData data, int stageIndex, int progressIndex, string routeId, LevelMapBuilder builder)
{
    // 1. Determine state
    StageState state = stageIndex < progressIndex  ? StageState.Completed
                     : stageIndex == progressIndex ? StageState.Current
                     :                               StageState.Locked;

    // 2. Set number text
    numberText.text = data.number;

    // 3. Set background color
    background.color = state == StageState.Completed ? new Color(0.12f, 0.12f, 0.12f)
                     : state == StageState.Current   ? Color.white
                     :                                 new Color(0.87f, 0.87f, 0.87f);

    // 4. Set number text color
    numberText.color = state == StageState.Completed ? Color.white
                     : state == StageState.Current   ? new Color(0.12f, 0.12f, 0.12f)
                     :                                 new Color(0.73f, 0.73f, 0.73f);

    // 5. Show correct icon
    doubleCheckIcon.SetActive(state == StageState.Completed);
    lockOpenIcon   .SetActive(state == StageState.Current);
    lockClosedIcon .SetActive(state == StageState.Locked);

    // 6. Wire click
    GetComponent<Button>().interactable = state != StageState.Locked;
    GetComponent<Button>().onClick.AddListener(() => {
        builder.AdvanceTo(routeId, stageIndex);
    });
}
```

Inspector references:
```csharp
public Image background;
public TMP_Text numberText;
public GameObject doubleCheckIcon;
public GameObject lockOpenIcon;
public GameObject lockClosedIcon;
```

---

### 4. `ProgressBar` update (inside LevelMapBuilder)

```csharp
void UpdateProgressBar(RouteGraph graph)
{
    int total     = graph.routes.Sum(r => r.stages.Count);
    int completed = graph.routes.Sum(r => r.stages.Count(s => s.state == StageState.Completed));
    progressBar.value = (float)completed / total;
}
```

---

## State Rules (critical)

```
progressIndex = 3  →  stages: [✓][✓][✓][🔓][🔒][🔒]
                                0   1   2   3   4   5

- Exactly ONE Current (open padlock) per route at all times
- All stages BEFORE progressIndex → Completed (black, double check)
- Stage AT progressIndex          → Current (white, open padlock, pulsing)
- All stages AFTER progressIndex  → Locked (grey, closed padlock, not clickable)

When player clicks stage N (completed or current):
  → builder.AdvanceTo(routeId, N)
  → progressIndex = N
  → all stages before N become Completed
  → stage N becomes Current
  → all stages after N become Locked
  → rebuild route visuals
```

---

## Integration with Procedural Generator

The generator must call:
```csharp
levelMapBuilder.Build(routeGraph);
```

The `RouteGraph` can contain any number of routes with any number of stages.
The builder handles layout automatically — no hardcoded positions.

When a stage is cleared in gameplay:
```csharp
levelMapBuilder.AdvanceTo(routeId, clearedStageIndex + 1);
// or re-generate and call Build() again if the graph structure changes
```

---

## Visual Reference

See `Level Map.html` in the project for the interactive HTML prototype.
It demonstrates all states, pan/zoom behavior, and progression logic exactly as it should work in Unity.

---

## What is NOT implemented yet (reserved for later)

- `StageModal` — popup panel when clicking a stage (concepts pending)
- Dashed line rendering for locked connectors (requires custom shader or UI Extensions)
- Stage pulse animation for Current state (use DOTween or Animator)
- Route labels displayed in the hatch border area
