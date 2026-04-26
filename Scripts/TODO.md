```
- doesnt specify that Tool.... components, should be added to the worldGameObjects in the Scene
- that inventoryTest require the Tool.... components to be connected to scene gameObjects and exist in scene prior.
```

```sceneHeirarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
rb = Rigidbody
bc = BoxCollider
sc = SphereCollider
mc = MeshCollider
alstn = AudioListener
cam = Camera
lgt = Light
canvas = Canvas
cr = CanvasRenderer
btn = Button | Image
img = Image
autoFitH = HorizontalLayoutGroup | ContentSizeFitter
ps = ParticleSystem
psr = ParticleSystemRenderer
lr = LineRenderer
================================

./InventoryTest/(scale:1.0 | no components)
├ Directional Light (scale:1.0 | lgt, UniversalAdditionalLightData)
├ Global Volume (scale:1.0 | Volume)
├ GameObject (scale:1.0 | no components)
├ Canvas (scale:1.5 | canvas, CanvasScaler, GraphicRaycaster, CanvasGroup)
│ ├ center Image (scale:1.0 | cr, img)
│ ├ fps Text (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ └ build phase (TMP)  (scale:1.0 | cr, TextMeshProUGUI)
│ ├ BgUI (Panel) (scale:1.0 | cr, img, bgUI)
│ ├ InventoryUI (Panel) (scale:1.0 | cr, img, InventoryUI, InventoryOrchestrator)
│ │ ├ HotBar (Panel) (scale:1.0 | cr, img, autoFitH, Outline)
│ │ │ ├ pfInventorySlot (Image) (scale:1.0 | cr, img, Field_InventorySlot)
│ │ │ │ ├ hideWhenDragged (scale:1.0 | no components)
│ │ │ │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │ │ │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ │ │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │ │ │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ │ │ └ organeBar (image) (scale:1.0 | cr, img)
│ │ │ └ pfInventorySlot (Image) (6) (scale:1.0 | cr, img, Field_InventorySlot)
│ │ │   ├ hideWhenDragged (scale:1.0 | no components)
│ │ │   │ ├ icon (image) (scale:1.0 | cr, img)
│ │ │   │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ │   │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │ │   │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ │   └ organeBar (image) (scale:1.0 | cr, img)
│ │ └ ExtendedBar (Panel) (scale:1.0 | cr, img, Outline, GridLayoutGroup)
│ │   ├ pfInventorySlot (Image) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (6) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (7) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (8) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (9) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (10) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (11) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (12) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (13) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (14) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (15) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   ├ pfInventorySlot (Image) (16) (scale:1.0 | cr, img, Field_InventorySlot)
│ │   │ ├ hideWhenDragged (scale:1.0 | no components)
│ │   │ │ ├ icon (image) (scale:1.0 | cr, img)
│ │   │ │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │   │ │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │   │ └ organeBar (image) (scale:1.0 | cr, img)
│ │   └ pfInventorySlot (Image) (17) (scale:1.0 | cr, img, Field_InventorySlot)
│ │     ├ hideWhenDragged (scale:1.0 | no components)
│ │     │ ├ icon (image) (scale:1.0 | cr, img)
│ │     │ ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │     │ └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│ │     │   └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │     └ organeBar (image) (scale:1.0 | cr, img)
│ ├ SelectedItemInfo (Panel) (scale:1.0 | cr, img, Field_SelectedItemInfo)
│ │ ├ Image (scale:1.0 | cr, img)
│ │ ├ name (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ ├ count (TMP) (1) (scale:1.0 | cr, TextMeshProUGUI)
│ │ ├ decr (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ ├ equipButton (Panel) (scale:1.0 | cr, img)
│ │ │ └ equipClick (Button) (scale:1.0 | cr, btn, autoFitH)
│ │ │   └ Text (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ │ └ dropButton (Panel) (scale:1.0 | cr, img)
│ │   └ dropClick (Button) (scale:1.0 | cr, btn, autoFitH)
│ │     └ Text (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│ └ dragGhost (Panel) (scale:1.0 | cr)
│   ├ icon (image) (scale:1.0 | cr, img)
│   ├ nameText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
│   └ countTxtBg (Panel) (scale:1.0 | cr, img, autoFitH)
│     └ countText (TMP) (scale:1.0 | cr, TextMeshProUGUI)
├ EventSystem (scale:1.0 | EventSystem, InputSystemUIInputModule)
├ Level (scale:1.0 | no components)
│ ├ Plane (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ Cube (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ Cube (1) (scale:(1.0,2.4,1.0) | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ computerScreen (scale:(0.8,1.0,0.1) | dmc, bc, ProBuilderMesh, ProBuilderShape)
│ ├ elevator (scale:1.0 | StartingElevator)
│ │ ├ elevatorPlatform (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ │ ├ playerTeleport (scale:1.0 | no components)
│ │ ├ elevator land (Particle System) (scale:1.0 | ps, psr)
│ │ └ walls (scale:1.0 | no components)
│ │   ├ elevatorWalls inside (scale:(1.0,1.0,1.0) | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ │   └ elevatorWalls outside (scale:(1.0,1.0,1.0) | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ Stairs (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ Cube (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ └ SpawnPoints (scale:1.0 | no components)
│   └ PlayerSpawnPoints (scale:1.0 | no components)
│     └ point (0) (scale:1.0 | PlayerSpawnPoint)
├ player (scale:1.0 | CharacterController, PlayerController, PlayerCamera, PlayerGrab)
│ ├ Main Camera (scale:1.0 | alstn, cam, UniversalAdditionalCameraData)
│ │ └ holdPos (scale:0.1 | dmc)
│ ├ model (scale:(0.2,1.0,0.2) | dmc, bc)
│ ├ viewModelContainer (scale:(0.2,1.0,0.2) | no components)
│ ├ rigidBodyDragger (scale:0.1 | no components)
│ ├ magnetToolPos (scale:0.1 | dmc, sc)
│ ├ nightVisionLight (scale:1.0 | lgt, UniversalAdditionalLightData)
│ ├ miningHatLight (scale:1.0 | lgt, UniversalAdditionalLightData)
│ ├ lrRope (scale:1.0 | lr)
│ └ groundCheck (scale:1.0 | no components)
├ Managers (scale:1.0 | no components)
│ ├ EconomyManager (scale:1.0 | EconomyManager)
│ ├ UIManager (scale:1.0 | UIManager)
│ ├ ShopSpawnPoints (scale:1.0 | no components)
│ │ └ point (0) (scale:1.0 | ShopSpawnPoint)
│ └ ObjectHighlighManager (scale:1.0 | ObjectHighlighterManager)
├ GRAB (scale:1.0 | no components)
│ ├ Sphere 0.5 (scale:0.5 | dmc, rb, sc, ProBuilderMesh, ProBuilderShape)
│ ├ Cube  0.1x0.4 (scale:(0.1,0.4,0.1) | dmc, rb, bc, ProBuilderMesh, ProBuilderShape)
│ ├ toolPickAxe (scale:(1.8,0.2,0.1) | ToolPickaxe)
│ │ ├ worldModel (scale:1.0 | dmc, rb, bc, ProBuilderMesh, ProBuilderShape)
│ │ └ viewModel (scale:1.0 | dmc, bc, ProBuilderMesh, ProBuilderShape)
│ └ toolHammer (scale:(0.6,0.2,0.1) | ToolHammer)
│   ├ worldModel (scale:1.0 | dmc, rb, bc, ProBuilderMesh, ProBuilderShape)
│   └ viewModel (scale:1.0 | dmc, bc, ProBuilderMesh, ProBuilderShape)
├ DEBUG_Check (scale:1.0 | InventoryTest)
└ Cube (scale:(0.5,1.0,1.0) | dmc, mc, ProBuilderMesh, ProBuilderShape)

```


```projectHeirarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
smc = SkinnedMeshRenderer
rb = Rigidbody
bc = BoxCollider
sc = SphereCollider
anim = Animator
asrc = AudioSource
cr = CanvasRenderer
sr = ScrollRect
btnO = Button | Image | Outline
img = Image
autoFitH = HorizontalLayoutGroup | ContentSizeFitter
================================

=== Asset Type Abbreviations ===
mesh = Mesh
mat = Material
pf = Prefab
tex = Texture
anim = AnimClip
audio = Audio
cs = Script
scene = Scene
txt = TextAsset
================================

./Assets/
├ AD/
│ └ Highlight Plus Profile - InteractableComputer.asset (HighlightProfile)
├ Export/
│ ├ phase-a complete (playerController, shopUI, interactionUI) Unity3D+6000.3.unitypackage (DefaultAsset)
│ └ phase-a-1 complete (startingElevator, camShake) Unity3D+6000.3.unitypackage (DefaultAsset)
├ HighlightPlus/
│ ├ Demo/
│ │ ├ Demo1_HighlightExample/
│ │ │ ├ LightingData.asset (LightingDataAsset)
│ │ │ ├ ReflectionProbe-0.exr (Cubemap)
│ │ │ └ ReflectionProbe-1.exr (Cubemap)
│ │ ├ Demo1_HighlightExample.unity (scene)
│ │ ├ Demo2_SelectionExample/
│ │ │ ├ LightingData.asset (LightingDataAsset)
│ │ │ ├ ReflectionProbe-0.exr (Cubemap)
│ │ │ └ ReflectionProbe-1.exr (Cubemap)
│ │ ├ Demo2_SelectionExample.unity (scene)
│ │ ├ Demo3_HitFXExample.unity (scene)
│ │ ├ Materials/
│ │ │ ├ Floor.mat (mat | URP/Lit)
│ │ │ ├ Gold.mat (mat | URP/Lit)
│ │ │ ├ PlasticGlossy.mat (mat | URP/Lit)
│ │ │ ├ Silk.mat (mat | URP/Lit)
│ │ │ └ Wall.mat (mat | URP/Lit)
│ │ ├ Profiles/
│ │ │ ├ Selected.asset (HighlightProfile)
│ │ │ ├ SelectedAndHighlighted.asset (HighlightProfile)
│ │ │ └ UniversalRenderPipelineGlobalSettings.asset (UniversalRenderPipelineGlobalSettings)
│ │ ├ Scripts/
│ │ │ ├ HitFxDemo.cs (cs | HitFxDemo)
│ │ │ ├ ManualSelectionDemo.cs (cs | ManualSelectionDemo)
│ │ │ ├ SphereHighlightEventExample.cs (cs | SphereHighlightEventExample)
│ │ │ └ SphereSelectionEventsExample.cs (cs | SphereSelectionEventsExample)
│ │ ├ Sounds/
│ │ │ └ metalHit.wav (audio | 0.30s | 2ch)
│ │ ├ Textures/
│ │ │ ├ floor_tiles_06_diff_1k.png (tex | 1024×1024 | DXT5)
│ │ │ ├ floor_tiles_06_nor_1k.png (tex | 1024×1024 | DXT5)
│ │ │ ├ overlaySampleTex.png (tex | 256×256 | DXT1)
│ │ │ ├ red_brick_plaster_patch_02_AO_1k.png (tex | 1024×1024 | DXT5)
│ │ │ ├ red_brick_plaster_patch_02_bump_1k.png (tex | 1024×1024 | DXT5)
│ │ │ ├ red_brick_plaster_patch_02_diff_1k.png (tex | 1024×1024 | DXT1)
│ │ │ └ red_brick_plaster_patch_02_Nor_1k.png (tex | 1024×1024 | DXT5)
│ │ └ URP settings/
│ │   ├ HighlightPlusForwardRenderer.asset (UniversalRendererData)
│ │   │ └ NewHighlightPlusRenderPassFeature (HighlightPlusRenderPassFeature)
│ │   └ UniversalRenderPipelineAsset.asset (UniversalRenderPipelineAsset)
│ ├ Documentation/
│ │ ├ Documentation Online.url (DefaultAsset)
│ │ ├ Documentation PDF.url (DefaultAsset)
│ │ └ Kronnect Assets.pdf (DefaultAsset)
│ ├ Editor/
│ │ ├ HighlightEffectEditor.cs (cs | HighlightEffectEditor)
│ │ ├ HighlightManagerEditor.cs (cs | HighlightManagerEditor)
│ │ ├ HighlightProfileEditor.cs (cs | HighlightProfileEditor)
│ │ ├ HighlightSeeThroughOccluderEditor.cs (cs | HighlightSeeThroughOccluderEditor)
│ │ ├ HighlightTriggerEditor.cs (cs | HighlightTriggerEditor)
│ │ └ TransparentWithDepth.cs (cs | TransparentWithDepth)
│ ├ README.txt (txt)
│ └ Runtime/
│   ├ Resources/
│   │ └ HighlightPlus/
│   │   ├ blueNoiseVL.png (tex | 32×32 | RGB24)
│   │   ├ CustomVertexTransform.cginc (txt)
│   │   ├ HighlightAddDepth.shader (Shader)
│   │   ├ HighlightBlockerOutlineAndGlow.mat (mat | HighlightPlus/UI/Mask)
│   │   ├ HighlightBlockerOverlay.mat (mat | HighlightPlus/UI/Mask)
│   │   ├ HighlightBlurGlow.shader (Shader)
│   │   ├ HighlightBlurOutline.shader (Shader)
│   │   ├ HighlightClearStencil.shader (Shader)
│   │   ├ HighlightComposeGlow.shader (Shader)
│   │   ├ HighlightComposeOutline.shader (Shader)
│   │   ├ HighlightGlow.mat (mat | HighlightPlus/Geometry/Glow)
│   │   ├ HighlightGlow.shader (Shader)
│   │   ├ HighlightIconFX.shader (Shader)
│   │   ├ HighlightInnerGlow.shader (Shader)
│   │   ├ HighlightMask.shader (Shader)
│   │   ├ HighlightOccluder.shader (Shader)
│   │   ├ HighlightOutline.mat (mat | HighlightPlus/Geometry/Outline)
│   │   ├ HighlightOutline.shader (Shader)
│   │   ├ HighlightOverlay.shader (Shader)
│   │   ├ HighlightPlusDepthWrite.mat (mat | HighlightPlus/Geometry/JustDepth)
│   │   ├ HighlightSeeThrough.shader (Shader)
│   │   ├ HighlightSeeThroughBorder.shader (Shader)
│   │   ├ HighlightSeeThroughMask.shader (Shader)
│   │   ├ HighlightSolidColor.shader (Shader)
│   │   ├ HighlightTarget.shader (Shader)
│   │   ├ HighlightUIMask.mat (mat | HighlightPlus/UI/Mask)
│   │   ├ HighlightUIMask.shader (Shader)
│   │   ├ IconMesh.fbx (pf | scale:1.0 | dmc)
│   │   │ └ Cone (mesh | bounds:0.20×0.40×0.20 | v:32)
│   │   └ target.png (tex | 256×256 | DXT5)
│   └ Scripts/
│     ├ HighlightEffect.cs (cs | HighlightEffect)
│     ├ HighlightEffectActions.cs (cs | unknown)
│     ├ HighlightEffectBlocker.cs (cs | HighlightEffectBlocker)
│     ├ HighlightEffectOccluderManager.cs (cs | unknown)
│     ├ HighlightManager.cs (cs | HighlightManager)
│     ├ HighlightPlusRenderPassFeature.cs (cs | HighlightPlusRenderPassFeature)
│     ├ HighlightProfile.cs (cs | HighlightProfile)
│     ├ HighlightSeeThroughOccluder.cs (cs | HighlightSeeThroughOccluder)
│     ├ HighlightTrigger.cs (cs | HighlightTrigger)
│     ├ InputProxy.cs (cs | InputProxy)
│     ├ Misc.cs (cs | Misc)
│     ├ RenderingUtils.cs (cs | RenderingUtils)
│     ├ ShaderParams.cs (cs | ShaderParams)
│     └ VRCheck.cs (cs | VRCheck)
├ Import/
│ └ --pack/
│   ├ learn.txt (txt)
│   └ web-tools.txt (txt)
├ LOG/
│ ├ GameData/
│ └ LOG.md (txt)
├ Prefabs/
│ ├ 3D/
│ │ ├ Cube  0.1x0.4.prefab (pf | scale:(0.1,0.4,0.1) | dmc, rb, bc, ProBuilderMesh, ProBuilderShape)
│ │ ├ Cube 1x1.prefab (pf | scale:1.0 | dmc, rb, bc, ProBuilderMesh, ProBuilderShape)
│ │ └ Sphere 0.5.prefab (pf | scale:0.5 | dmc, rb, sc, ProBuilderMesh, ProBuilderShape)
│ └ UI/
│   ├ button (Panel).prefab (pf | scale:1.0 | cr, img)
│   ├ pfCategory (Panel).prefab (pf | scale:1.0 | cr, img, Field_ShopCategory)
│   ├ pfInteractionOption (Panel).prefab (pf | scale:1.0 | cr, img, Field_InteractionOption)
│   ├ pfInventorySlot (Image).prefab (pf | scale:1.0 | cr, img, Field_InventorySlot)
│   ├ pfShopCartItem(Panel) .prefab (pf | scale:1.0 | cr, img, Field_ShopCartItem)
│   └ pfShopItem(Panel).prefab (pf | scale:1.0 | cr, img, Field_ShopItem)
├ ProBuilder Data/
│ ├ Default Color Palette.asset (ColorPalette)
│ └ Default Material Palette.asset (MaterialPalette)
├ Scenes/
│ ├ phase-a complete.unity (scene)
│ ├ phase-a-1 complete.unity (scene)
│ ├ phase-b.unity (scene)
│ ├ SampleScene.unity (scene)
│ └ VerticalSliceTest/
│   └ phase-b --inventory.unity (scene)
├ Scripts/
│ ├ DEBUG_Check.cs (cs | DEBUG_Check)
│ ├ phase-@all/
│ │ ├ 0-Core/
│ │ │ ├ GameEvents.cs (cs | GameEvents)
│ │ │ └ Singleton.cs (cs | unknown)
│ │ ├ 1-Managers/
│ │ │ ├ EconomyManager.cs (cs | EconomyManager)
│ │ │ └ UIManager.cs (cs | UIManager)
│ │ ├ 2-Data/
│ │ │ └ Enums/
│ │ │   └ GlobalEnums.cs (cs | unknown)
│ │ └ 4-Utils/
│ │   ├ IEnumerableUtilsPhaseAll.cs (cs | IEnumerableUtilsPhaseAll)
│ │   └ UtilsPhaseAll.cs (cs | UtilsPhaseAll)
│ ├ phase-a/
│ │ ├ 0-Core/
│ │ │ └ GameEvents.cs (cs | GameEvents)
│ │ ├ 1-Managers/
│ │ │ └ SubManager/
│ │ │   ├ bgUI.cs (cs | bgUI)
│ │ │   ├ InteractionWheelUI.cs (cs | InteractionWheelUI)
│ │ │   ├ moneyUI.cs (cs | moneyUI)
│ │ │   └ ShopUI.cs (cs | ShopUI)
│ │ ├ 2-Data/
│ │ │ ├ DataServices/
│ │ │ │ └ ShopDataService.cs (cs | ShopDataService)
│ │ │ ├ DataWrappers/
│ │ │ │ └ ShopDataWrapper.cs (cs | WShopItem)
│ │ │ ├ Enums/
│ │ │ ├ Field_InteractionOption.cs (cs | Field_InteractionOption)
│ │ │ ├ Field_ShopCartItem.cs (cs | Field_ShopCartItem)
│ │ │ ├ Field_ShopCategory.cs (cs | Field_ShopCategory)
│ │ │ ├ Field_ShopItem.cs (cs | Field_ShopItem)
│ │ │ ├ Interfaces/
│ │ │ │ └ IInteractable.cs (cs | IInteractable)
│ │ │ ├ SO_InteractionOption.cs (cs | SO_InteractionOption)
│ │ │ ├ SO_ShopCategory.cs (cs | SO_ShopCategory)
│ │ │ └ SO_ShopItemDef.cs (cs | SO_ShopItemDef)
│ │ ├ 3-MonoBehaviours/
│ │ │ ├ InteractableComputer.cs (cs | InteractableComputer)
│ │ │ ├ Orchestrator/
│ │ │ │ └ ShopUIOrchestrator.cs (cs | ShopUIOrchestrator)
│ │ │ ├ ShopSpawnPoint.cs (cs | ShopSpawnPoint)
│ │ │ ├ SimplePlayerController.cs (cs | SimplePlayerController)
│ │ │ └ SimplePlayerInteraction.cs (cs | SimplePlayerInteraction)
│ │ ├ 4-Utils/
│ │ │ ├ PhaseALOG.cs (cs | PhaseALOG)
│ │ │ └ UtilsPhaseA.cs (cs | UtilsPhaseA)
│ │ └ 5-Tests/
│ │   └ ShopUITest.cs (cs | ShopUITest)
│ ├ phase-a-1/
│ │ ├ 0-Core/
│ │ │ └ GameEvents.cs (cs | GameEvents)
│ │ ├ 1-Managers/
│ │ ├ 2-Data/
│ │ ├ 3-Monobehaviours/
│ │ │ ├ CameraShaker.cs (cs | CameraShaker)
│ │ │ └ StartingElevator.cs (cs | StartingElevator)
│ │ ├ 4-Utils/
│ │ └ 5-Tests/
│ │   └ ElevatorTest.cs (cs | ElevatorTest)
│ ├ phase-b/
│ │ ├ 0-Core/
│ │ │ └ GameEvents.cs (cs | GameEvents)
│ │ ├ 1-Managers/
│ │ │ ├ ObjectHighlighterManager.cs (cs | ObjectHighlighterManager)
│ │ │ └ SubManagers/
│ │ │   └ InventoryUI.cs (cs | InventoryUI)
│ │ ├ 2-Data/
│ │ │ ├ 2-DataWrapper/
│ │ │ ├ 3-Entities/
│ │ │ ├ DataServices/
│ │ │ │ └ InventoryDataService.cs (cs | InventoryDataService)
│ │ │ ├ Entities/
│ │ │ │ └ InventorySlot.cs (cs | InventorySlot)
│ │ │ ├ Enums/
│ │ │ │ └ GlobalEnums.cs (cs | unknown)
│ │ │ ├ Field_InventorySlot.cs (cs | Field_InventorySlot)
│ │ │ ├ Field_SelectedItemInfo.cs (cs | Field_SelectedItemInfo)
│ │ │ ├ Interfaces/
│ │ │ │ └ GlobalInterfaces.cs (cs | IHighlightable)
│ │ │ └ SO_FootStepSoundDef.cs (cs | SO_FootStepSoundDef)
│ │ ├ 3-Monobehaviours/
│ │ │ ├ DomainSpecific/
│ │ │ ├ Orchestrators/
│ │ │ │ └ InventoryOrchestrator.cs (cs | InventoryOrchestrator)
│ │ │ ├ Physics/
│ │ │ │ ├ BasePhysicsObject.cs (cs | BasePhysicsObject)
│ │ │ │ └ BaseSellableItem.cs (cs | BaseSellableItem)
│ │ │ ├ Player/
│ │ │ │ ├ PlayerCamera.cs (cs | PlayerCamera)
│ │ │ │ ├ PlayerController.cs (cs | PlayerController)
│ │ │ │ ├ PlayerGrab.cs (cs | PlayerGrab)
│ │ │ │ ├ PlayerSpawnPoint.cs (cs | PlayerSpawnPoint)
│ │ │ │ └ RbDraggerController.cs (cs | RbDraggerController)
│ │ │ ├ Tool/
│ │ │ │ ├ BaseHeldTool.cs (cs | BaseHeldTool)
│ │ │ │ ├ ToolBuilder.cs (cs | ToolBuilder)
│ │ │ │ ├ ToolHammer.cs (cs | ToolHammer)
│ │ │ │ └ ToolPickAxe.cs (cs | ToolPickaxe)
│ │ │ ├ UI/
│ │ │ │ └ UIEventRelay.cs (cs | UIEventRelay)
│ │ │ └ UIRelays/
│ │ ├ 4-Utils/
│ │ │ ├ PhaseBLOG.cs (cs | PhaseBLOG)
│ │ │ └ UtilsPhaseB.cs (cs | UtilsPhaseB)
│ │ ├ 5-Tests/
│ │ │ ├ InventoryTest.cs (cs | InventoryTest)
│ │ │ ├ Manual/
│ │ │ ├ PlayerControllerTest.cs (cs | PlayerControllerTest)
│ │ │ └ ToolActionTest.cs (cs | ToolActionTest)
│ │ └ DEBUG_CheckB.cs (cs | DEBUG_CheckB)
│ ├ phase-x/
│ │ ├ 0-Core/
│ │ │ └ GameEvents.cs (cs | GameEvents)
│ │ ├ 1-Managers/
│ │ │ └ SubManagers/
│ │ ├ 2-Data/
│ │ │ ├ DataServices/
│ │ │ ├ DataWrappers/
│ │ │ ├ Entities/
│ │ │ ├ Enums/
│ │ │ └ Interfaces/
│ │ ├ 3-Monobehaviours/
│ │ │ ├ DomainSpecific/
│ │ │ ├ Orchestrators/
│ │ │ └ UIRelays/
│ │ ├ 4-Utils/
│ │ └ 5-Tests/
│ │   └ Manual/
│ └ TODO.md (txt)
├ SO/
│ ├ SO_FootStepSoundDef player.asset (SO_FootStepSoundDef)
│ ├ SO_InteractionOption grab.asset (SO_InteractionOption)
│ ├ SO_InteractionOption openShopView.asset (SO_InteractionOption)
│ ├ SO_ShopCategory explosives.asset (SO_ShopCategory)
│ ├ SO_ShopCategory tools.asset (SO_ShopCategory)
│ ├ SO_ShopItemDef dynamite.asset (SO_ShopItemDef)
│ ├ SO_ShopItemDef lamp.asset (SO_ShopItemDef)
│ └ SO_ShopItemDef pickAxe.asset (SO_ShopItemDef)
├ TM/
│ ├ FONTS 1/
│ │ ├ CONSOLA SDF.asset (TMP_FontAsset)
│ │ │ ├ CONSOLA SDF Material (mat | TextMeshPro/Distance Field)
│ │ │ └ CONSOLA SDF Atlas (tex | 512×512 | Alpha8)
│ │ ├ CONSOLA.TTF (Font)
│ │ │ ├ Font Material (mat | GUI/Text Shader)
│ │ │ └ Font Texture (tex | 256×256 | Alpha8)
│ │ ├ CONSOLAI.TTF (Font)
│ │ │ ├ Font Material (mat | GUI/Text Shader)
│ │ │ └ Font Texture (tex | 256×256 | Alpha8)
│ │ └ pixelplay.ttf (Font)
│ │   ├ Font Material (mat | GUI/Text Shader)
│ │   └ Font Texture (tex | 256×256 | Alpha8)
│ └ TextMesh Pro/
│   ├ Fonts/
│   │ ├ LiberationSans - OFL.txt (txt)
│   │ └ LiberationSans.ttf (Font)
│   │   ├ Font Material (mat | GUI/Text Shader)
│   │   └ Font Texture (tex | 256×256 | Alpha8)
│   ├ Resources/
│   │ ├ Fonts & Materials/
│   │ │ ├ LiberationSans SDF - Drop Shadow.mat (mat | TextMeshPro/Mobile/Distance Field)
│   │ │ ├ LiberationSans SDF - Fallback.asset (TMP_FontAsset)
│   │ │ │ ├ LiberationSans SDF Material (mat | TextMeshPro/Mobile/Distance Field)
│   │ │ │ └ LiberationSans SDF Atlas (tex | 0×0 | Alpha8)
│   │ │ ├ LiberationSans SDF - Outline.mat (mat | TextMeshPro/Mobile/Distance Field)
│   │ │ └ LiberationSans SDF.asset (TMP_FontAsset)
│   │ │   ├ LiberationSans SDF Material (mat | TextMeshPro/Mobile/Distance Field)
│   │ │   └ LiberationSans SDF Atlas (tex | 1024×1024 | Alpha8)
│   │ ├ LineBreaking Following Characters.txt (txt)
│   │ ├ LineBreaking Leading Characters.txt (txt)
│   │ ├ Sprite Assets/
│   │ │ └ EmojiOne.asset (TMP_SpriteAsset)
│   │ ├ Style Sheets/
│   │ │ └ Default Style Sheet.asset (TMP_StyleSheet)
│   │ └ TMP Settings.asset (TMP_Settings)
│   ├ Shaders/
│   │ ├ SDFFunctions.hlsl (txt)
│   │ ├ TMPro.cginc (txt)
│   │ ├ TMPro_Mobile.cginc (txt)
│   │ ├ TMPro_Properties.cginc (txt)
│   │ ├ TMPro_Surface.cginc (txt)
│   │ ├ TMP_Bitmap-Custom-Atlas.shader (Shader)
│   │ ├ TMP_Bitmap-Mobile.shader (Shader)
│   │ ├ TMP_Bitmap.shader (Shader)
│   │ ├ TMP_SDF Overlay.shader (Shader)
│   │ ├ TMP_SDF SSD.shader (Shader)
│   │ ├ TMP_SDF-HDRP LIT.shadergraph (Shader)
│   │ │ └ TMP_SDF-HDRP LIT (mat | TextMeshPro/SRP/TMP_SDF-HDRP LIT)
│   │ ├ TMP_SDF-HDRP UNLIT.shadergraph (Shader)
│   │ │ └ TMP_SDF-HDRP UNLIT (mat | TextMeshPro/SRP/TMP_SDF-HDRP UNLIT)
│   │ ├ TMP_SDF-Mobile Masking.shader (Shader)
│   │ ├ TMP_SDF-Mobile Overlay.shader (Shader)
│   │ ├ TMP_SDF-Mobile SSD.shader (Shader)
│   │ ├ TMP_SDF-Mobile-2-Pass.shader (Shader)
│   │ ├ TMP_SDF-Mobile.shader (Shader)
│   │ ├ TMP_SDF-Surface-Mobile.shader (Shader)
│   │ ├ TMP_SDF-Surface.shader (Shader)
│   │ ├ TMP_SDF-URP Lit.shadergraph (Shader)
│   │ │ └ TMP_SDF-URP Lit (mat | TextMeshPro/SRP/TMP_SDF-URP Lit)
│   │ ├ TMP_SDF-URP Unlit.shadergraph (Shader)
│   │ │ └ TMP_SDF-URP Unlit (mat | TextMeshPro/SRP/TMP_SDF-URP Unlit)
│   │ ├ TMP_SDF.shader (Shader)
│   │ └ TMP_Sprite.shader (Shader)
│   └ Sprites/
│     ├ EmojiOne Attribution.txt (txt)
│     ├ EmojiOne.json (txt)
│     └ EmojiOne.png (tex | 512×512 | DXT5)
├ URP/
│ ├ Readme.asset (Readme)
│ ├ Settings/
│ │ ├ DefaultVolumeProfile.asset (VolumeProfile)
│ │ ├ Mobile_Renderer.asset (UniversalRendererData)
│ │ ├ Mobile_RPAsset.asset (UniversalRenderPipelineAsset)
│ │ ├ PC_Renderer.asset (UniversalRendererData)
│ │ │ └ ScreenSpaceAmbientOcclusion (ScreenSpaceAmbientOcclusion)
│ │ ├ PC_RPAsset.asset (UniversalRenderPipelineAsset)
│ │ ├ SampleSceneProfile.asset (VolumeProfile)
│ │ └ UniversalRenderPipelineGlobalSettings.asset (UniversalRenderPipelineGlobalSettings)
│ └ TutorialInfo/
│   ├ Icons/
│   │ └ URP.png (tex | 350×200 | RGB24)
│   ├ Layout.wlt (DefaultAsset)
│   └ Scripts/
│     ├ Editor/
│     │ └ ReadmeEditor.cs (cs | ReadmeEditor)
│     └ Readme.cs (cs | Readme)

```

```
also the phase-a-1 doesnt depend on phase-a (update the dependency graph-grid)
```

```
why the guide/(Test/Manual/*.md) wasn't consise enough for the scene to work ?
```
