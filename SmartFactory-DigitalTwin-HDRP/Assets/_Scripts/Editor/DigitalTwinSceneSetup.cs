using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using SmartFactory.DigitalTwin.Data;
using SmartFactory.DigitalTwin.Navigation;
using SmartFactory.DigitalTwin.Visuals;
using SmartFactory.DigitalTwin.UI;

namespace SmartFactory.DigitalTwin.Editor
{
    public class DigitalTwinSceneSetup : EditorWindow
    {
        [MenuItem("Tools/Digital Twin/Setup Digital Twin in Current Scene")]
        public static void SetupScene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Digital Twin Scene");
            int group = Undo.GetCurrentGroup();

            Debug.Log("[Digital Twin Setup] Starting automated robotics factory configuration...");

            // 1. Create / Configure DigitalTwinManager
            GameObject managerObj = GameObject.Find("DigitalTwinManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("DigitalTwinManager");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create DigitalTwinManager");
            }

            if (managerObj.GetComponent<TelemetrySimulatorEngine>() == null)
            {
                Undo.AddComponent<TelemetrySimulatorEngine>(managerObj);
            }
            if (managerObj.GetComponent<MQTTClientBridge>() == null)
            {
                Undo.AddComponent<MQTTClientBridge>(managerObj);
            }

            // 2. Setup Camera Rig calibrated to factory interior
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            // Disable player walk scripts if present
            var playerWalks = FindObjectsByType<UnityFactorySceneHDRP.CameraMove>(FindObjectsSortMode.None);
            foreach (var pw in playerWalks)
            {
                pw.enabled = false;
            }

            var camCtrl = mainCam.GetComponent<DigitalTwinCameraController>();
            if (camCtrl == null)
            {
                camCtrl = Undo.AddComponent<DigitalTwinCameraController>(mainCam.gameObject);
            }
            camCtrl.InitializeFactoryPresets();

            if (mainCam.GetComponent<PhysicsRaycaster>() == null)
            {
                Undo.AddComponent<PhysicsRaycaster>(mainCam.gameObject);
            }

            if (mainCam.GetComponent<Optimization.LowEndOptimizerRuntime>() == null)
            {
                Undo.AddComponent<Optimization.LowEndOptimizerRuntime>(mainCam.gameObject);
            }

            // 3. Locate and Setup Station Visualizers inside factory
            SetupStationVisualizers();
            camCtrl.RebindToActualSceneObjects();

            // 4. Build Complete UI HUD Canvas
            BuildDigitalTwinHUD();

            // 5. Ensure EventSystem exists and cleanup all legacy StandaloneInputModule
            SetupModernEventSystem();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayDialog("Robotics Digital Twin Setup Complete",
                "🤖 Smart Robotics & Automation Digital Twin has been configured!\n\n" +
                "• Perfectly aligned dark-glass UI HUD\n" +
                "• 5 Cinematic 3/4 Isometric Camera Presets\n" +
                "• S-Curve Kinematics & Realistic Telemetry Active\n" +
                "• Modern InputSystem UI Module configured\n\n" +
                "Press PLAY to experience your Robotics Factory Digital Twin!", "OK");
        }

        private static void SetupModernEventSystem()
        {
            var legacyModules = FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None);
            foreach (var lm in legacyModules)
            {
                Undo.DestroyObjectImmediate(lm);
            }

            EventSystem es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                es = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }
            else
            {
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
                }
            }
        }

        private static void SetupStationVisualizers()
        {
            // Remove old bulky 3D world text badges
            var allGos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                if (go.name == "FloatingBadgeAnchor" || go.name == "StationWorldBadge")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            Vector3 posRobot1 = FindObjectPosition("Arm", new Vector3(20.5f, 0f, 25.0f));
            Vector3 posRobot2 = FindObjectPosition("Line_03", new Vector3(28.5f, 0f, 42.0f));
            Vector3 posConveyor = FindObjectPosition("Line_07", new Vector3(22.0f, 0f, 28.0f));
            Vector3 posPLC = FindObjectPosition("Controller_1", new Vector3(12.8f, 0f, 17.8f));

            CreateOrUpdateStationVisualizer("Station_01_Robot", "ST-01", "6-Axis Robot Assembly Cell 1", StationType.RoboticAssembly, posRobot1);
            CreateOrUpdateStationVisualizer("Station_02_Welding", "ST-02", "Robotic Welding & Fastening", StationType.RoboticWelding, posRobot2);
            CreateOrUpdateStationVisualizer("Station_03_Conveyor", "ST-03", "Conveyor & Vision Sorting Line", StationType.ConveyorSorting, posConveyor);
            CreateOrUpdateStationVisualizer("Station_04_PLC", "ST-04", "Central PLC & Safety Substation", StationType.FacilityPLC, posPLC);
        }

        private static Vector3 FindObjectPosition(string namePrefix, Vector3 fallback)
        {
            var gos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in gos)
            {
                if (go.name.StartsWith(namePrefix) && !go.name.StartsWith("Station_"))
                {
                    return go.transform.position;
                }
            }
            return fallback;
        }

        private static void CreateOrUpdateStationVisualizer(string objName, string id, string displayName, StationType type, Vector3 position)
        {
            GameObject stationObj = GameObject.Find(objName);
            if (stationObj == null)
            {
                stationObj = new GameObject(objName);
                Undo.RegisterCreatedObjectUndo(stationObj, $"Create {objName}");
            }

            stationObj.transform.position = position;

            var vis = stationObj.GetComponent<StationVisualizer>();
            if (vis == null)
            {
                vis = Undo.AddComponent<StationVisualizer>(stationObj);
            }

            SerializedObject so = new SerializedObject(vis);
            so.FindProperty("stationId").stringValue = id;
            so.FindProperty("stationDisplayName").stringValue = displayName;
            so.FindProperty("stationType").enumValueIndex = (int)type;
            so.ApplyModifiedProperties();
        }

        private static void BuildDigitalTwinHUD()
        {
            GameObject existingCanvas = GameObject.Find("DigitalTwinHUDCanvas");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // Canvas
            GameObject canvasObj = new GameObject("DigitalTwinHUDCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create DigitalTwinHUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            DigitalTwinDashboardHUD hud = canvasObj.AddComponent<DigitalTwinDashboardHUD>();
            SerializedObject so = new SerializedObject(hud);

            // 1. Top Header Bar (0 to 50px from top)
            GameObject header = CreatePanel(canvasObj.transform, "HeaderBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 50), new Color(0.04f, 0.07f, 0.12f, 0.90f));
            
            var titleText = CreateText(header.transform, "TitleText", "🤖 SMART ROBOTICS FACTORY DIGITAL TWIN <color=#00E5FF>• INDUSTRY 4.0</color>", 16, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(25, 0), new Vector2(550, 36));
            titleText.fontStyle = FontStyles.Bold;

            var statusDot = CreateImage(header.transform, "StatusDot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-160, 0), new Vector2(12, 12), new Color(0.2f, 0.95f, 0.4f));
            var statusText = CreateText(header.transform, "StatusText", "ROBOTICS FLEET: NOMINAL", 13, TextAlignmentOptions.MidlineLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(-145, 0), new Vector2(300, 26));
            statusText.color = new Color(0.2f, 0.95f, 0.4f);

            var clockText = CreateText(header.transform, "ClockText", "2026-08-31 00:00:00", 13, TextAlignmentOptions.MidlineRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-25, 0), new Vector2(220, 26));
            clockText.color = new Color(0.6f, 0.8f, 1f);

            so.FindProperty("clockText").objectReferenceValue = clockText;
            so.FindProperty("facilityStatusText").objectReferenceValue = statusText;
            so.FindProperty("facilityStatusDot").objectReferenceValue = statusDot;

            // 2. Navigation Tab Bar (Top Left, 60px from top)
            GameObject navBar = CreatePanel(canvasObj.transform, "NavBar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, -60), new Vector2(620, 36), new Color(0.05f, 0.09f, 0.15f, 0.80f));
            HorizontalLayoutGroup navHlg = navBar.AddComponent<HorizontalLayoutGroup>();
            navHlg.spacing = 6;
            navHlg.padding = new RectOffset(6, 6, 4, 4);
            navHlg.childForceExpandWidth = true;
            navHlg.childForceExpandHeight = true;

            var btnOverview = CreateButton(navBar.transform, "BtnOverview", "🌐 Overview");
            var btnStation1 = CreateButton(navBar.transform, "BtnStation1", "🤖 Robot Cell 1");
            var btnStation2 = CreateButton(navBar.transform, "BtnStation2", "⚡ Welding Arm");
            var btnStation3 = CreateButton(navBar.transform, "BtnStation3", "📦 Conveyor");
            var btnStation4 = CreateButton(navBar.transform, "BtnStation4", "🎛️ Central PLC");

            so.FindProperty("btnOverview").objectReferenceValue = btnOverview;
            so.FindProperty("btnStation1").objectReferenceValue = btnStation1;
            so.FindProperty("btnStation2").objectReferenceValue = btnStation2;
            so.FindProperty("btnStation3").objectReferenceValue = btnStation3;
            so.FindProperty("btnStation4").objectReferenceValue = btnStation4;

            // 3. Left Panel - Selected Robotic Station Telemetry Card (Pinned Top-Left)
            GameObject mainCard = CreatePanel(canvasObj.transform, "MainStationCard", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, -105), new Vector2(340, 720), new Color(0.04f, 0.07f, 0.12f, 0.82f));
            
            // Station Title & Badge
            var stationTitle = CreateText(mainCard.transform, "StationTitle", "6-Axis Robot Assembly Cell 1", 17, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(210, 42));
            stationTitle.fontStyle = FontStyles.Bold;
            so.FindProperty("stationTitleText").objectReferenceValue = stationTitle;

            var stateBadge = CreatePanel(mainCard.transform, "StateBadge", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(88, 24), new Color(0.1f, 0.9f, 0.4f, 0.2f));
            var stateBadgeText = CreateText(stateBadge.transform, "Text", "RUNNING", 11, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stateBadgeText.fontStyle = FontStyles.Bold;
            stateBadgeText.color = new Color(0.2f, 0.95f, 0.4f);
            so.FindProperty("machineStateBadgeText").objectReferenceValue = stateBadgeText;
            so.FindProperty("machineStateBadgeBg").objectReferenceValue = stateBadge.GetComponent<Image>();

            // OEE Section
            CreateText(mainCard.transform, "OeeHeader", "OVERALL ROBOTIC EFFECTIVENESS (OEE)", 10, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -64), new Vector2(308, 16)).color = new Color(0.5f, 0.7f, 0.9f);
            var oeeVal = CreateText(mainCard.transform, "OeeValue", "98.2%", 28, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -82), new Vector2(140, 34));
            oeeVal.fontStyle = FontStyles.Bold;
            oeeVal.color = new Color(0.0f, 0.9f, 1.0f);
            so.FindProperty("oeeValueText").objectReferenceValue = oeeVal;

            var oeeBarBg = CreatePanel(mainCard.transform, "OeeBarBg", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -120), new Vector2(308, 6), new Color(0.1f, 0.15f, 0.22f, 1f));
            var oeeBarFill = CreateImage(oeeBarBg.transform, "OeeBarFill", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, new Color(0.0f, 0.85f, 1.0f));
            oeeBarFill.type = Image.Type.Filled;
            oeeBarFill.fillMethod = Image.FillMethod.Horizontal;
            oeeBarFill.fillAmount = 0.98f;
            so.FindProperty("oeeProgressBar").objectReferenceValue = oeeBarFill;

            var availText = CreateText(mainCard.transform, "AvailText", "Avail: 98.4%", 11, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -132), new Vector2(96, 18));
            var perfText = CreateText(mainCard.transform, "PerfText", "Perf: 96.5%", 11, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -132), new Vector2(96, 18));
            var qualText = CreateText(mainCard.transform, "QualText", "Qual: 99.6%", 11, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, -132), new Vector2(96, 18));
            availText.color = perfText.color = qualText.color = new Color(0.7f, 0.85f, 1f);

            so.FindProperty("availabilityText").objectReferenceValue = availText;
            so.FindProperty("performanceText").objectReferenceValue = perfText;
            so.FindProperty("qualityText").objectReferenceValue = qualText;

            // Robotics Kinematics & Velocity
            CreateText(mainCard.transform, "VelHeader", "ROBOT JOINT VELOCITY / BELT SPEED", 10, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -160), new Vector2(308, 16)).color = new Color(0.5f, 0.7f, 0.9f);
            var velVal = CreateText(mainCard.transform, "VelValue", "175 <size=13>°/s</size>", 24, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -178), new Vector2(180, 30));
            velVal.fontStyle = FontStyles.Bold;
            velVal.color = new Color(0.2f, 1.0f, 0.4f);
            so.FindProperty("jointVelocityText").objectReferenceValue = velVal;

            var torqueText = CreateText(mainCard.transform, "TorqueText", "Torque: 42.0 Nm", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -212), new Vector2(145, 18));
            var cycleTime = CreateText(mainCard.transform, "CycleTime", "Cycle: 7.0s", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(175, -212), new Vector2(145, 18));
            torqueText.color = cycleTime.color = new Color(0.7f, 0.85f, 1f);
            so.FindProperty("motorTorqueText").objectReferenceValue = torqueText;
            so.FindProperty("cycleTimeText").objectReferenceValue = cycleTime;

            // Joint Temperature
            CreateText(mainCard.transform, "TempHeader", "SERVO MOTOR TEMPERATURE (JOINT 3)", 10, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -240), new Vector2(308, 16)).color = new Color(0.5f, 0.7f, 0.9f);
            var tempVal = CreateText(mainCard.transform, "TempValue", "45.2°C", 22, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -258), new Vector2(140, 26));
            tempVal.fontStyle = FontStyles.Bold;
            tempVal.color = new Color(0.3f, 0.85f, 1f);
            so.FindProperty("tempValueText").objectReferenceValue = tempVal;

            var tempBarBg = CreatePanel(mainCard.transform, "TempBarBg", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -290), new Vector2(308, 6), new Color(0.1f, 0.15f, 0.22f, 1f));
            var tempBarFill = CreateImage(tempBarBg.transform, "TempBarFill", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.8f, 1.0f));
            tempBarFill.type = Image.Type.Filled;
            tempBarFill.fillMethod = Image.FillMethod.Horizontal;
            tempBarFill.fillAmount = 0.45f;
            so.FindProperty("tempProgressBar").objectReferenceValue = tempBarFill;

            // Diagnostics Grid
            var payload = CreateText(mainCard.transform, "PayloadText", "Payload: 14.5 kg", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -308), new Vector2(145, 18));
            var ampsText = CreateText(mainCard.transform, "AmpsText", "Current: 10.8 A", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(175, -308), new Vector2(145, 18));
            var wearText = CreateText(mainCard.transform, "WearText", "Gripper: 16.0%", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -332), new Vector2(145, 18));
            var partsText = CreateText(mainCard.transform, "PartsText", "Units: 482 / 600", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(175, -332), new Vector2(145, 18));
            payload.color = ampsText.color = wearText.color = partsText.color = new Color(0.7f, 0.85f, 1f);

            so.FindProperty("payloadText").objectReferenceValue = payload;
            so.FindProperty("currentAmpsText").objectReferenceValue = ampsText;
            so.FindProperty("toolWearText").objectReferenceValue = wearText;
            so.FindProperty("partsCounterText").objectReferenceValue = partsText;

            // 4. Bottom Center - Live Waveform Graph (Joint Torques)
            GameObject graphCard = CreatePanel(canvasObj.transform, "GraphCard", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(620, 200), new Color(0.04f, 0.07f, 0.12f, 0.85f));
            
            var graphTitle = CreateText(graphCard.transform, "GraphTitle", "REAL-TIME ROBOT JOINT DYNAMIC TORQUE <color=#7FA5C5>(J1, J2, J3 Channels in Nm)</color>", 11, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -10), new Vector2(440, 20));
            graphTitle.fontStyle = FontStyles.Bold;

            var torqueMag = CreateText(graphCard.transform, "TorqueMag", "42.0 Nm", 15, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -10), new Vector2(140, 22));
            torqueMag.fontStyle = FontStyles.Bold;
            torqueMag.color = new Color(0.2f, 0.95f, 0.4f);
            so.FindProperty("torqueMagnitudeText").objectReferenceValue = torqueMag;

            // Chart area
            GameObject chartObj = new GameObject("LiveWaveformChart");
            chartObj.transform.SetParent(graphCard.transform, false);
            RectTransform chartRt = chartObj.AddComponent<RectTransform>();
            chartRt.anchorMin = new Vector2(0, 0);
            chartRt.anchorMax = new Vector2(1, 1);
            chartRt.offsetMin = new Vector2(16, 32);
            chartRt.offsetMax = new Vector2(-16, -34);
            LiveWaveformChart chart = chartObj.AddComponent<LiveWaveformChart>();
            so.FindProperty("waveformChart").objectReferenceValue = chart;

            // Graph legend
            CreateText(graphCard.transform, "Legend", "<color=#33D6FF>■ J1 Axis</color>   <color=#33FF66>■ J2 Axis</color>   <color=#FFBF33>■ J3 Axis</color>   <color=#FF3333>-- Torque Overload Limit</color>", 10, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(16, 12), new Vector2(-32, 18));

            // 5. Right Panel - Robotics Fault Injection & Safety Console (Pinned Top-Right)
            GameObject rightPanel = CreatePanel(canvasObj.transform, "RightDiagnosticPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-25, -60), new Vector2(340, 765), new Color(0.04f, 0.07f, 0.12f, 0.82f));
            
            // Fault Injection Header
            var faultTitle = CreateText(rightPanel.transform, "FaultTitle", "ROBOTICS FAULT & SCENARIO TESTING", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(308, 22));
            faultTitle.fontStyle = FontStyles.Bold;
            faultTitle.color = new Color(1.0f, 0.75f, 0.2f);

            var btnOverheat = CreateButton(rightPanel.transform, "BtnOverheat", "🔥 Simulate Joint Overheat");
            SetElementDirect(btnOverheat.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -44), new Vector2(308, 34));

            var btnTorque = CreateButton(rightPanel.transform, "BtnTorque", "⚡ Inject Torque Overload");
            SetElementDirect(btnTorque.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -84), new Vector2(308, 34));

            var btnJam = CreateButton(rightPanel.transform, "BtnJam", "📦 Trigger Conveyor Jam");
            SetElementDirect(btnJam.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -124), new Vector2(308, 34));

            var btnEStop = CreateButton(rightPanel.transform, "BtnEStop", "🛑 EMERGENCY STOP (Safety Curtain)");
            SetElementDirect(btnEStop.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -164), new Vector2(308, 34));
            btnEStop.GetComponent<Image>().color = new Color(0.7f, 0.1f, 0.1f, 0.85f);

            var btnReset = CreateButton(rightPanel.transform, "BtnReset", "🔄 Reset All Faults");
            SetElementDirect(btnReset.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -204), new Vector2(308, 34));

            so.FindProperty("btnInjectOverheat").objectReferenceValue = btnOverheat;
            so.FindProperty("btnInjectTorqueOverload").objectReferenceValue = btnTorque;
            so.FindProperty("btnInjectConveyorJam").objectReferenceValue = btnJam;
            so.FindProperty("btnEmergencyStop").objectReferenceValue = btnEStop;
            so.FindProperty("btnResetFaults").objectReferenceValue = btnReset;

            // Alarm Log Console
            var logTitle = CreateText(rightPanel.transform, "LogTitle", "LIVE SAFETY & EVENT CONSOLE", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -252), new Vector2(308, 22));
            logTitle.fontStyle = FontStyles.Bold;
            logTitle.color = new Color(0.4f, 0.85f, 1.0f);

            var logBox = CreatePanel(rightPanel.transform, "LogBox", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.04f, 0.07f, 0.95f));
            RectTransform logBoxRt = logBox.GetComponent<RectTransform>();
            logBoxRt.offsetMin = new Vector2(16, 16);
            logBoxRt.offsetMax = new Vector2(-16, -280);

            var logText = CreateText(logBox.transform, "LogText", "Waiting for robotics telemetry...", 11, TextAlignmentOptions.TopLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform logTextRt = logText.GetComponent<RectTransform>();
            logTextRt.offsetMin = new Vector2(10, 10);
            logTextRt.offsetMax = new Vector2(-10, -10);
            logText.enableWordWrapping = true;
            so.FindProperty("alarmLogText").objectReferenceValue = logText;

            // 6. Part Inspection Modal (Center Overlay)
            GameObject modal = CreatePanel(canvasObj.transform, "PartInspectionPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 230), new Color(0.05f, 0.10f, 0.18f, 0.96f));
            modal.SetActive(false);

            var pTitle = CreateText(modal.transform, "ModalTitle", "ROBOTIC COMPONENT DIAGNOSTICS", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -14), new Vector2(344, 22));
            pTitle.fontStyle = FontStyles.Bold;
            pTitle.color = new Color(0.0f, 0.9f, 1.0f);

            var pName = CreateText(modal.transform, "PartName", "J3 Harmonic Drive Servo Motor", 15, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -40), new Vector2(344, 22));
            pName.fontStyle = FontStyles.Bold;

            var pNum = CreateText(modal.transform, "PartNum", "Part #: RB-720-J3", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -68), new Vector2(344, 18));
            var pMfr = CreateText(modal.transform, "PartMfr", "OEM: KUKA / Yaskawa", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -90), new Vector2(344, 18));
            var pHours = CreateText(modal.transform, "PartHours", "Operating Hours: 3,420 hrs", 12, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -112), new Vector2(344, 18));
            var pHealth = CreateText(modal.transform, "PartHealth", "Health Score: 98.4%", 14, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -138), new Vector2(344, 22));
            pHealth.fontStyle = FontStyles.Bold;
            pHealth.color = new Color(0.2f, 0.95f, 0.4f);

            var btnClosePart = CreateButton(modal.transform, "BtnClose", "Close Inspection");
            SetElementDirect(btnClosePart.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-36, 32));

            so.FindProperty("partInspectionPanel").objectReferenceValue = modal;
            so.FindProperty("partNameText").objectReferenceValue = pName;
            so.FindProperty("partNumberText").objectReferenceValue = pNum;
            so.FindProperty("partManufacturerText").objectReferenceValue = pMfr;
            so.FindProperty("partHoursText").objectReferenceValue = pHours;
            so.FindProperty("partHealthText").objectReferenceValue = pHealth;
            so.FindProperty("btnClosePartInspection").objectReferenceValue = btnClosePart;

            so.ApplyModifiedProperties();
        }

        #region UI Creation Helpers

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 32);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.10f, 0.16f, 0.25f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.12f, 0.18f, 0.28f, 0.85f);
            cb.highlightedColor = new Color(0.18f, 0.28f, 0.42f, 0.95f);
            cb.pressedColor = new Color(0.08f, 0.45f, 0.75f, 1f);
            btn.colors = cb;

            var txt = CreateText(btnObj.transform, "Label", label, 11, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.85f, 0.95f, 1f);

            return btn;
        }

        private static void SetElementDirect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = sizeDelta;
            }
        }

        #endregion
    }
}
