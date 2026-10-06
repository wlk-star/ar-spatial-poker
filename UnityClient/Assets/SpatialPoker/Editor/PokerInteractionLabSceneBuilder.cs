using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using SpatialPoker.App;
using SpatialPoker.AR;
using SpatialPoker.HandTracking;
using SpatialPoker.Interaction;
using SpatialPoker.Networking;
using SpatialPoker.Presentation.Cards;
using SpatialPoker.Presentation.Table;
using SpatialPoker.UI;

namespace SpatialPoker.Editor
{
    /// <summary>
    /// One-click builders for the poker lab scenes.
    /// <list type="bullet">
    /// <item>Menu SpatialPoker/Build PokerInteractionLab Scene: fixed-camera Editor
    /// lab with mouse hand-tracking (existing behavior).</item>
    /// <item>Menu SpatialPoker/Build AR Poker Scene: phone AR scene with
    /// AR Session + XR Origin, tap-to-place table via
    /// <see cref="ARTablePlacementController"/>, same network/table/HUD stack.
    /// In the Editor (no AR hardware) the table drops at a fixed pose so the
    /// scene stays usable.</item>
    /// </list>
    /// Both builders create the procedural table/card/chip art materials and
    /// wire every serialized reference explicitly. For the AR scene, press Play
    /// with the Node server reachable (set the server address in the HUD on the
    /// phone; 127.0.0.1 won't work there).
    /// </summary>
    public static class PokerInteractionLabSceneBuilder
    {
        private const string ScenePath = "Assets/SpatialPoker/Scenes/PokerInteractionLab.unity";
        private const string ARScenePath = "Assets/SpatialPoker/Scenes/ARPokerLab.unity";
        private const string ARPlanePrefabPath = "Assets/SpatialPoker/Prefabs/ARPlane.prefab";

        [MenuItem("SpatialPoker/Build PokerInteractionLab Scene")]
        public static void Build()
        {
            BuildScene(ScenePath, arMode: false);
        }

        [MenuItem("SpatialPoker/Build AR Poker Scene")]
        public static void BuildARScene()
        {
            BuildScene(ARScenePath, arMode: true);
        }

        private static void BuildScene(string scenePath, bool arMode)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureEventSystem();
            var mats = EnsureMaterials();
            TuneLighting();

            Camera camera;
            ARTablePlacementController placement = null;
            if (arMode)
            {
                var defaultCam = GameObject.Find("Main Camera");
                if (defaultCam != null)
                    Object.DestroyImmediate(defaultCam);
                camera = BuildARFoundation(out placement);
            }
            else
            {
                camera = Object.FindFirstObjectByType<Camera>();
                camera.transform.SetPositionAndRotation(
                    new Vector3(0f, 2.2f, -2.8f), Quaternion.Euler(38f, 0f, 0f));
                camera.fieldOfView = 25f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.085f, 0.11f);
            }

            // ---------- Network stack ----------
            var net = BuildNetworkStack();

            // ---------- Lab bootstrap ----------
            var bootstrapGo = new GameObject("LabBootstrap");
            var bootstrap = bootstrapGo.AddComponent<LocalPokerLabBootstrap>();
            SetRef(bootstrap, "session", net.session);
            SetRef(bootstrap, "synchronizer", net.synchronizer);

            // ---------- Table ----------
            var table = BuildTable(mats);
            var tableTopTransform = table.tableTop.transform;
            if (arMode)
            {
                // Placed by tap via ARTablePlacementController (or at the
                // fallback pose in the Editor).
                table.tableRoot.SetActive(false);
                SetRef(placement, "arCamera", camera);
                SetRef(placement, "sceneTableRoot", table.tableRoot.transform);
            }

            // ---------- Interaction ----------
            if (!arMode)
            {
                var interaction = new GameObject("Interaction");
                var resolver = interaction.AddComponent<InteractionResolver>();
                var snapManager = interaction.AddComponent<SnapManager>();
                var gestureController = interaction.AddComponent<GestureInteractionController>();
                var mouseProvider = interaction.AddComponent<MockMouseHandTrackingProvider>();

                SetRef(gestureController, "provider", mouseProvider);
                SetRef(gestureController, "resolver", resolver);
                SetRef(mouseProvider, "interactionCamera", camera);
                SetRef(mouseProvider, "interactionPlane", tableTopTransform);
            }

            // ---------- Presentation ----------
            BuildPresentation(net, table, mats);

            // ---------- HUD ----------
            var hud = BuildHud();
            WireHud(hud, net);

            // ---------- Save ----------
            SaveScene(scene, scenePath);
            Debug.Log($"[SpatialPoker] {(arMode ? "AR" : "Lab")} scene built at {scenePath}. " +
                      (arMode
                          ? "On a phone, set the server address in the HUD, tap a detected plane to place the table."
                          : "Start the Node server (npm start), press Play, and use the mouse to pinch."));
        }

        // ----- AR foundation -----

        private static Camera BuildARFoundation(out ARTablePlacementController placement)
        {
            var sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();

            var originGo = new GameObject("XR Origin");
            var origin = originGo.AddComponent<XROrigin>();

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originGo.transform, false);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(cameraOffset.transform, false);
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.05f;
            var poseDriver = camGo.AddComponent<TrackedPoseDriver>();
            var positionAction = new InputAction(
                "Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotationAction = new InputAction(
                "Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            poseDriver.positionInput = new InputActionProperty(positionAction);
            poseDriver.rotationInput = new InputActionProperty(rotationAction);
            camGo.AddComponent<ARCameraManager>();
            camGo.AddComponent<ARCameraBackground>();
            camGo.AddComponent<AudioListener>();

            origin.CameraFloorOffsetObject = cameraOffset;
            origin.Camera = camera;

            originGo.AddComponent<ARRaycastManager>();
            var planeManager = originGo.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = EnsureARPlanePrefab();
            originGo.AddComponent<ARAnchorManager>();
            originGo.AddComponent<TrackingStateGuard>();

            placement = originGo.AddComponent<ARTablePlacementController>();
            return camera;
        }

        private static GameObject EnsureARPlanePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARPlanePrefabPath);
            if (prefab != null)
                return prefab;

            EnsureFolder("Assets/SpatialPoker/Prefabs");
            var go = new GameObject("ARPlane");
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EnsureARPlaneMaterial();
            go.AddComponent<ARPlane>();
            go.AddComponent<ARPlaneMeshVisualizer>();
            prefab = PrefabUtility.SaveAsPrefabAsset(go, ARPlanePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Material EnsureARPlaneMaterial()
        {
            const string path = "Assets/SpatialPoker/Materials/Table/ARPlane.mat";
            EnsureFolder("Assets/SpatialPoker/Materials/Table");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            mat.SetFloat("_Mode", 3f); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0.35f, 0.65f, 1f, 0.22f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.6f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ----- extracted build steps -----

        private sealed class NetworkRefs
        {
            public ClientWebSocketTransport transport;
            public GameStateSynchronizer synchronizer;
            public PokerActionClient actionClient;
            public PokerSessionClient session;
            public LegalActionGate gate;
            public PokerIntentNormalizer normalizer;
            public LegalActionIntentSink sink;
            public PokerActionBridge bridge;
        }

        private static NetworkRefs BuildNetworkStack()
        {
            var net = new NetworkRefs();
            var network = new GameObject("Network");
            net.transport = network.AddComponent<ClientWebSocketTransport>();
            net.synchronizer = network.AddComponent<GameStateSynchronizer>();
            net.actionClient = network.AddComponent<PokerActionClient>();
            net.session = network.AddComponent<PokerSessionClient>();
            net.gate = network.AddComponent<LegalActionGate>();
            net.normalizer = network.AddComponent<PokerIntentNormalizer>();
            net.sink = network.AddComponent<LegalActionIntentSink>();
            net.bridge = network.AddComponent<PokerActionBridge>();

            SetRef(net.session, "transport", net.transport);
            SetRef(net.session, "synchronizer", net.synchronizer);
            SetRef(net.session, "actionClient", net.actionClient);
            SetRef(net.actionClient, "synchronizer", net.synchronizer);
            SetRef(net.gate, "synchronizer", net.synchronizer);
            SetRef(net.normalizer, "gate", net.gate);
            SetRef(net.normalizer, "downstreamSinkBehaviour", net.sink);
            SetRef(net.sink, "gate", net.gate);
            SetRef(net.sink, "downstreamSinkBehaviour", net.actionClient);
            SetRef(net.bridge, "intentSinkBehaviour", net.normalizer);
            SetRef(net.bridge, "gate", net.gate);
            return net;
        }

        private sealed class TableRefs
        {
            public GameObject tableRoot;
            public GameObject tableTop;
            public TMP_Text[] boardLabels;
            public GameObject[] boardSlots;
            public Renderer[] boardRenderers;
            public TMP_Text potLabel;
            public TMP_Text[] holeLabels;
            public GameObject[] holeSlots;
            public Renderer[] holeRenderers;
            public GameObject[] opponentBacks;
        }

        private static TableRefs BuildTable(MaterialSet mats)
        {
            var table = new TableRefs();
            var tableRoot = new GameObject("TableRoot");
            table.tableRoot = tableRoot;

            var tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(tableRoot.transform);
            tableTop.transform.localScale = new Vector3(1.4f, 0.04f, 1.0f);
            tableTop.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            tableTop.GetComponent<Renderer>().sharedMaterial = mats.felt;
            table.tableTop = tableTop;

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(tableRoot.transform);
            rail.transform.localScale = new Vector3(1.56f, 0.07f, 1.16f);
            rail.transform.localPosition = new Vector3(0f, -0.075f, 0f);
            rail.GetComponent<Renderer>().sharedMaterial = mats.wood;

            // Board zone: 5 fixed community-card slots.
            var boardZone = Child(tableRoot, "PublicZone/BoardZone");
            boardZone.transform.localPosition = new Vector3(0f, 0.005f, -0.28f);
            table.boardLabels = new TMP_Text[5];
            table.boardSlots = new GameObject[5];
            table.boardRenderers = new Renderer[5];
            for (var i = 0; i < 5; i++)
            {
                var slot = CardPrimitive(boardZone, $"BoardSlot{i}", (i - 2) * 0.075f, mats.cardFaceTemplate);
                table.boardSlots[i] = slot;
                table.boardRenderers[i] = slot.GetComponent<Renderer>();
                table.boardLabels[i] = CardLabel(slot, $"BoardLabel{i}");
            }

            // Pot label.
            var potZone = Child(tableRoot, "PublicZone/PotZone");
            potZone.transform.localPosition = new Vector3(0f, 0.005f, -0.14f);
            table.potLabel = WorldLabel(potZone, "PotLabel", "POT 0", 0.02f, 36f, 0.011f);

            // Local seat: 2 private cards + chip home + betting zone.
            var localSeat = Child(tableRoot, "LocalSeat");
            localSeat.transform.localPosition = new Vector3(0f, 0.005f, 0.28f);
            table.holeLabels = new TMP_Text[2];
            table.holeSlots = new GameObject[2];
            table.holeRenderers = new Renderer[2];
            for (var i = 0; i < 2; i++)
            {
                var slot = CardPrimitive(
                    localSeat, $"HoleCardSlot{(char)('A' + i)}", (i - 0.5f) * 0.075f, mats.cardFaceTemplate);
                table.holeSlots[i] = slot;
                table.holeRenderers[i] = slot.GetComponent<Renderer>();
                table.holeLabels[i] = CardLabel(slot, $"HoleLabel{i}");
            }

            var chipHome = Child(localSeat, "ChipHome");
            chipHome.transform.localPosition = new Vector3(0.38f, 0f, 0f);
            BuildChipStack(chipHome, new[] { mats.chipWhite, mats.chipRed, mats.chipBlue, mats.chipGreen });
            var chipGroup = chipHome.AddComponent<Presentation.Chips.ChipGroup>();
            SetRef(chipGroup, "homeAnchor", chipHome.transform);

            var bettingZoneGo = Child(localSeat, "BettingZone");
            bettingZoneGo.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            var zoneCollider = bettingZoneGo.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(0.5f, 0.1f, 0.2f);
            bettingZoneGo.AddComponent<BettingZone>();

            // Opponent seat: 2 card backs only (values never leave the server).
            var opponentSeat = Child(tableRoot, "OpponentSeat");
            opponentSeat.transform.localPosition = new Vector3(0f, 0.005f, -0.42f);
            table.opponentBacks = new GameObject[2];
            for (var i = 0; i < 2; i++)
            {
                var back = CardPrimitive(
                    opponentSeat, $"OpponentBack{i}", (i - 0.5f) * 0.075f, mats.cardBack);
                table.opponentBacks[i] = back;
            }

            var opponentChipHome = Child(opponentSeat, "ChipHome");
            opponentChipHome.transform.localPosition = new Vector3(0.38f, 0f, 0f);
            BuildChipStack(opponentChipHome, new[] { mats.chipBlack, mats.chipBlue, mats.chipRed });

            return table;
        }

        private static void BuildPresentation(NetworkRefs net, TableRefs table, MaterialSet mats)
        {
            var presentation = new GameObject("Presentation");
            var boardPresentation = presentation.AddComponent<BoardPresentation>();
            var communityBoard = presentation.AddComponent<CommunityBoardPresentation>();
            var opponentBacksPresentation = presentation.AddComponent<OpponentCardBackPresentation>();
            var holeBinder = presentation.AddComponent<LocalHoleCardsBinder>();

            SetRef(boardPresentation, "potText", table.potLabel);
            SetRef(communityBoard, "synchronizer", net.synchronizer);
            SetRefArray(communityBoard, "cardLabels", table.boardLabels);
            SetRefArray(communityBoard, "cardSlots", table.boardSlots);
            SetRefArray(communityBoard, "cardRenderers", table.boardRenderers);
            SetRef(communityBoard, "cardFaceTemplate", mats.cardFaceTemplate);
            SetRef(opponentBacksPresentation, "synchronizer", net.synchronizer);
            SetRefArray(opponentBacksPresentation, "cardBacks", table.opponentBacks);
            SetRef(holeBinder, "synchronizer", net.synchronizer);
            SetRef(holeBinder, "firstCardText", table.holeLabels[0]);
            SetRef(holeBinder, "secondCardText", table.holeLabels[1]);
            SetRefArray(holeBinder, "cardSlots", table.holeSlots);
            SetRefArray(holeBinder, "cardRenderers", table.holeRenderers);
            SetRef(holeBinder, "cardFaceTemplate", mats.cardFaceTemplate);
        }

        private static void WireHud(HudRefs refs, NetworkRefs net)
        {
            var hud = refs.root;
            var legalHud = hud.AddComponent<LegalActionHud>();
            var labHud = hud.AddComponent<PokerLabHudController>();
            var serverPanel = hud.AddComponent<ServerAddressPanel>();

            SetRef(legalHud, "gate", net.gate);
            SetRef(legalHud, "foldButton", refs.foldButton);
            SetRef(legalHud, "checkButton", refs.checkButton);
            SetRef(legalHud, "callButton", refs.callButton);
            SetRef(legalHud, "betRaiseButton", refs.betSubmitButton);
            SetRef(legalHud, "allInButton", refs.allInButton);
            SetRef(legalHud, "callLabel", refs.callLabel);
            SetRef(legalHud, "betRaiseLabel", refs.betAmountText);

            SetRef(labHud, "synchronizer", net.synchronizer);
            SetRef(labHud, "transport", net.transport);
            SetRef(labHud, "gate", net.gate);
            SetRef(labHud, "bridge", net.bridge);
            SetRef(labHud, "statusText", refs.statusText);
            SetRef(labHud, "streetText", refs.streetText);
            SetRef(labHud, "actorText", refs.actorText);
            SetRef(labHud, "roomCodeText", refs.roomCodeText);
            SetRef(labHud, "betSlider", refs.betSlider);
            SetRef(labHud, "betAmountText", refs.betAmountText);
            SetRef(labHud, "betSubmitButton", refs.betSubmitButton);
            SetRef(labHud, "foldButton", refs.foldButton);
            SetRef(labHud, "checkButton", refs.checkButton);
            SetRef(labHud, "callButton", refs.callButton);
            SetRef(labHud, "allInButton", refs.allInButton);

            SetRef(serverPanel, "transport", net.transport);
            SetRef(serverPanel, "session", net.session);
            SetRef(serverPanel, "addressInput", refs.serverInput);
            SetRef(serverPanel, "applyButton", refs.serverApplyButton);
        }

        private static void SaveScene(Scene scene, string scenePath)
        {
            EditorSceneManager.SaveScene(scene, scenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private sealed class MaterialSet
        {
            public Material felt;
            public Material wood;
            public Material cardBack;
            public Material cardFaceTemplate;
            public Material chipWhite;
            public Material chipRed;
            public Material chipBlue;
            public Material chipGreen;
            public Material chipBlack;
        }

        private static MaterialSet EnsureMaterials()
        {
            return new MaterialSet
            {
                felt = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Table/Felt.mat", "Assets/SpatialPoker/Textures/Table/felt.png", Color.white, 0.9f),
                wood = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Table/Wood.mat", "Assets/SpatialPoker/Textures/Table/wood.png", new Color(0.43f, 0.24f, 0.12f), 0.35f),
                cardBack = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Cards/CardBack.mat", "Assets/SpatialPoker/Textures/Cards/Resources/CardFaces/card_back.png", Color.white, 0.35f),
                cardFaceTemplate = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Cards/CardFace.mat", null, Color.white, 0.35f),
                chipWhite = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Chips/ChipWhite.mat", "Assets/SpatialPoker/Textures/Chips/chip_white.png", Color.white, 0.3f),
                chipRed = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Chips/ChipRed.mat", "Assets/SpatialPoker/Textures/Chips/chip_red.png", Color.white, 0.3f),
                chipBlue = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Chips/ChipBlue.mat", "Assets/SpatialPoker/Textures/Chips/chip_blue.png", Color.white, 0.3f),
                chipGreen = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Chips/ChipGreen.mat", "Assets/SpatialPoker/Textures/Chips/chip_green.png", Color.white, 0.3f),
                chipBlack = GetOrCreateMaterial("Assets/SpatialPoker/Materials/Chips/ChipBlack.mat", "Assets/SpatialPoker/Textures/Chips/chip_black.png", Color.white, 0.3f),
            };
        }

        private static Material GetOrCreateMaterial(
            string matPath, string texturePath, Color color, float smoothness)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(matPath).Replace('\\', '/'));
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
                if (shader == null)
                    throw new System.InvalidOperationException("No supported material shader was found.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, matPath);
                material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            material.color = color;
            if (string.IsNullOrEmpty(texturePath))
            {
                material.mainTexture = null;
            }
            else
            {
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (material.mainTexture == null)
                    Debug.LogError($"[SpatialPoker] Texture asset was not imported: {texturePath}");
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void TuneLighting()
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.96f, 0.89f);
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.46f);
        }

        private static void BuildChipStack(GameObject parent, Material[] chipMaterials)
        {
            for (var i = 0; i < chipMaterials.Length; i++)
            {
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                chip.name = $"Chip{i}";
                chip.transform.SetParent(parent.transform);
                chip.transform.localScale = new Vector3(0.035f, 0.004f, 0.035f);
                chip.transform.localPosition = new Vector3(0f, 0.004f + i * 0.0085f, 0f);
                chip.GetComponent<Renderer>().sharedMaterial = chipMaterials[i];
            }
        }

        // ----- helpers -----

        private static GameObject Child(GameObject parent, string path)
        {
            var current = parent.transform;
            foreach (var part in path.Split('/'))
            {
                var next = current.Find(part);
                if (next == null)
                {
                    var go = new GameObject(part);
                    go.transform.SetParent(current);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    next = go.transform;
                }
                current = next;
            }
            return current.gameObject;
        }

        private static GameObject CardPrimitive(GameObject parent, string name, float x, Material material)
        {
            var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = name;
            card.transform.SetParent(parent.transform);
            card.transform.localScale = new Vector3(0.062f, 0.002f, 0.088f);
            card.transform.localPosition = new Vector3(x, 0.002f, 0f);
            card.transform.localRotation = Quaternion.identity;
            card.GetComponent<Renderer>().sharedMaterial = material;
            return card;
        }

        private static TMP_Text CardLabel(GameObject card, string name)
        {
            var label = WorldLabel(card, name, string.Empty, 0.012f, 30f, 0.0055f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.12f, 0.14f, 0.16f);
            return label;
        }

        private static TMP_Text WorldLabel(
            GameObject parent, string name, string text, float y, float fontSize, float worldScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * worldScale;
            var tmp = go.AddComponent<TextMeshPro>();
            if (tmp.font == null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(400f, 90f);
            return tmp;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private sealed class HudRefs
        {
            public GameObject root;
            public TMP_Text statusText;
            public TMP_Text streetText;
            public TMP_Text actorText;
            public TMP_Text roomCodeText;
            public Slider betSlider;
            public TMP_Text betAmountText;
            public Button betSubmitButton;
            public Button foldButton;
            public Button checkButton;
            public Button callButton;
            public Button allInButton;
            public TMP_Text callLabel;
            public TMP_InputField serverInput;
            public Button serverApplyButton;
        }

        private static HudRefs BuildHud()
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var refs = new HudRefs { root = canvasGo };

            refs.statusText = UiLabel(canvasGo, "StatusText", new Vector2(16, -16), TextAnchor.UpperLeft);
            refs.roomCodeText = UiLabel(canvasGo, "RoomCodeText", new Vector2(16, -44), TextAnchor.UpperLeft);
            refs.streetText = UiLabel(canvasGo, "StreetText", new Vector2(16, -72), TextAnchor.UpperLeft);
            refs.actorText = UiLabel(canvasGo, "ActorText", new Vector2(16, -100), TextAnchor.UpperLeft);

            // Server address row (top-right). Phone builds can't use 127.0.0.1.
            refs.serverInput = UiInputField(canvasGo, "ServerInput", new Vector2(-16, -16), 320f);
            refs.serverApplyButton = UiTopRightButton(canvasGo, "ServerApplyButton", "Apply", new Vector2(-348, -16), 110f);

            const float buttonWidth = 130f;
            const float buttonHeight = 56f;
            const float spacing = 12f;
            var totalWidth = buttonWidth * 5 + spacing * 4;
            var startX = -totalWidth / 2f;

            refs.foldButton = UiButton(canvasGo, "FoldButton", "Fold", startX, buttonWidth, buttonHeight);
            refs.checkButton = UiButton(canvasGo, "CheckButton", "Check", startX + (buttonWidth + spacing), buttonWidth, buttonHeight);
            refs.callButton = UiButton(canvasGo, "CallButton", "Call", startX + (buttonWidth + spacing) * 2, buttonWidth, buttonHeight);
            refs.betSubmitButton = UiButton(canvasGo, "BetButton", "Bet", startX + (buttonWidth + spacing) * 3, buttonWidth, buttonHeight);
            refs.allInButton = UiButton(canvasGo, "AllInButton", "All-In", startX + (buttonWidth + spacing) * 4, buttonWidth, buttonHeight);

            refs.callLabel = refs.callButton.GetComponentInChildren<TMP_Text>();

            // Bet slider above the buttons.
            var sliderGo = new GameObject("BetSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0f);
            sliderRect.anchorMax = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = new Vector2(0f, 150f);
            sliderRect.sizeDelta = new Vector2(420f, 28f);
            refs.betSlider = sliderGo.AddComponent<Slider>();

            var sliderBackground = UiImage(sliderGo.transform, "Background");
            sliderBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            var fill = UiImage(fillArea.transform, "Fill");
            fill.color = new Color(0.28f, 0.76f, 0.58f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = UiImage(handleArea.transform, "Handle");
            handle.color = new Color(0.94f, 0.93f, 0.87f, 1f);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(20f, 28f);

            refs.betSlider.fillRect = fillRect;
            refs.betSlider.handleRect = handleRect;
            refs.betSlider.targetGraphic = handle;
            refs.betSlider.direction = Slider.Direction.LeftToRight;

            refs.betAmountText = UiLabel(canvasGo, "BetAmountText", new Vector2(0, 186), TextAnchor.LowerCenter);
            refs.betAmountText.text = string.Empty;

            return refs;
        }

        private static TMP_Text UiLabel(GameObject canvas, string name, Vector2 anchoredPos, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            var lowerCenter = anchor == TextAnchor.LowerCenter;
            rect.anchorMin = lowerCenter ? new Vector2(0.5f, 0f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = lowerCenter ? new Vector2(0.5f, 0f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(360f, 30f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (tmp.font == null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 24;
            tmp.alignment = anchor == TextAnchor.LowerCenter
                ? TextAlignmentOptions.Center
                : TextAlignmentOptions.Left;
            tmp.text = name;
            return tmp;
        }

        private static Button UiButton(GameObject canvas, string name, string label, float x, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x + width / 2f, 90f);
            rect.sizeDelta = new Vector2(width, height);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.2f, 0.85f);
            var button = go.AddComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (tmp.font == null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;

            return button;
        }

        private static Button UiTopRightButton(
            GameObject canvas, string name, string label, Vector2 anchoredPos, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(width, 44f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.45f, 0.32f, 0.9f);
            var button = go.AddComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (tmp.font == null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;

            return button;
        }

        private static TMP_InputField UiInputField(
            GameObject canvas, string name, Vector2 anchoredPos, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(width, 44f);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var input = go.AddComponent<TMP_InputField>();

            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            var areaRect = textArea.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10f, 6f);
            areaRect.offsetMax = new Vector2(-10f, -6f);
            textArea.AddComponent<RectMask2D>();

            var placeholder = UiFieldText(textArea, "Placeholder", "ws://192.168.1.10:8080");
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            var text = UiFieldText(textArea, "Text", string.Empty);
            text.color = Color.white;

            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = TMP_Settings.defaultFontAsset;
            input.pointSize = 22;
            return input;
        }

        private static TMP_Text UiFieldText(GameObject parent, string name, string content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (tmp.font == null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 22;
            tmp.text = content;
            return tmp;
        }

        private static Image UiImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[SpatialPoker] Missing property '{property}' on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[SpatialPoker] Missing array property '{property}' on {target.GetType().Name}.");
                return;
            }
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
