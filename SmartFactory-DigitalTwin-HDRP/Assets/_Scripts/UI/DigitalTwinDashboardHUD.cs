using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SmartFactory.DigitalTwin.Data;
using SmartFactory.DigitalTwin.Navigation;
using SmartFactory.DigitalTwin.Visuals;

namespace SmartFactory.DigitalTwin.UI
{
    public class DigitalTwinDashboardHUD : MonoBehaviour
    {
        public static DigitalTwinDashboardHUD Instance { get; private set; }

        [Header("Selected Station")]
        [SerializeField] private string currentSelectedStationId = "ST-01";

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI facilityStatusText;
        [SerializeField] private Image facilityStatusDot;

        [Header("Station Selector Buttons")]
        [SerializeField] private Button btnOverview;
        [SerializeField] private Button btnStation1;
        [SerializeField] private Button btnStation2;
        [SerializeField] private Button btnStation3;
        [SerializeField] private Button btnStation4;

        [Header("Main Telemetry Panel")]
        [SerializeField] private TextMeshProUGUI stationTitleText;
        [SerializeField] private TextMeshProUGUI machineStateBadgeText;
        [SerializeField] private Image machineStateBadgeBg;

        [Header("OEE Metrics")]
        [SerializeField] private TextMeshProUGUI oeeValueText;
        [SerializeField] private Image oeeProgressBar;
        [SerializeField] private TextMeshProUGUI availabilityText;
        [SerializeField] private TextMeshProUGUI performanceText;
        [SerializeField] private TextMeshProUGUI qualityText;

        [Header("Robotics Kinematics")]
        [SerializeField] private TextMeshProUGUI jointVelocityText;
        [SerializeField] private Image velocityRadialFill;
        [SerializeField] private TextMeshProUGUI motorTorqueText;
        [SerializeField] private TextMeshProUGUI cycleTimeText;

        [Header("Sensors & Diagnostics")]
        [SerializeField] private TextMeshProUGUI tempValueText;
        [SerializeField] private Image tempProgressBar;
        [SerializeField] private TextMeshProUGUI payloadText;
        [SerializeField] private TextMeshProUGUI currentAmpsText;
        [SerializeField] private TextMeshProUGUI toolWearText;
        [SerializeField] private TextMeshProUGUI partsCounterText;

        [Header("Waveform Graph")]
        [SerializeField] private LiveWaveformChart waveformChart;
        [SerializeField] private TextMeshProUGUI torqueMagnitudeText;

        [Header("Machine-Specific Fault Injection Buttons")]
        [SerializeField] private Button btnInjectOverheat;        // Fault 1
        [SerializeField] private Button btnInjectTorqueOverload;  // Fault 2
        [SerializeField] private Button btnInjectConveyorJam;     // Fault 3
        [SerializeField] private Button btnEmergencyStop;
        [SerializeField] private Button btnResetFaults;

        [Header("Alarm / Event Console")]
        [SerializeField] private TextMeshProUGUI alarmLogText;
        [SerializeField] private int maxLogLines = 8;
        private readonly List<string> alarmLogs = new List<string>();

        [Header("Part Inspection Panel")]
        [SerializeField] private GameObject partInspectionPanel;
        [SerializeField] private TextMeshProUGUI partNameText;
        [SerializeField] private TextMeshProUGUI partNumberText;
        [SerializeField] private TextMeshProUGUI partManufacturerText;
        [SerializeField] private TextMeshProUGUI partHoursText;
        [SerializeField] private TextMeshProUGUI partHealthText;
        [SerializeField] private Button btnClosePartInspection;

        private readonly Color btnNormalColor = new Color(0.10f, 0.16f, 0.25f, 0.85f);
        private readonly Color btnWarningActiveColor = new Color(0.85f, 0.50f, 0.05f, 0.95f);
        private readonly Color btnCriticalActiveColor = new Color(0.85f, 0.15f, 0.15f, 0.95f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            WireUIEvents();
        }

        private void Start()
        {
            if (TelemetrySimulatorEngine.Instance != null)
            {
                TelemetrySimulatorEngine.Instance.OnStationTelemetryUpdated += HandleStationUpdated;
                TelemetrySimulatorEngine.Instance.OnAlarmRaised += HandleAlarmRaised;
                TelemetrySimulatorEngine.Instance.OnFaultsChanged += UpdateFaultButtonVisuals;
            }

            PartInspectableRaycaster.OnPartSelected += HandlePartInspected;

            AddLogEntry("SYSTEM", "Smart Robotics Digital Twin initialized.", AlarmSeverity.Info);
            SelectStation("ST-01");
        }

        private void OnDestroy()
        {
            if (TelemetrySimulatorEngine.Instance != null)
            {
                TelemetrySimulatorEngine.Instance.OnStationTelemetryUpdated -= HandleStationUpdated;
                TelemetrySimulatorEngine.Instance.OnAlarmRaised -= HandleAlarmRaised;
                TelemetrySimulatorEngine.Instance.OnFaultsChanged -= UpdateFaultButtonVisuals;
            }

            PartInspectableRaycaster.OnPartSelected -= HandlePartInspected;
        }

        private void WireUIEvents()
        {
            if (btnOverview != null) btnOverview.onClick.AddListener(() => FocusStationAndCamera(0, "ST-01"));
            if (btnStation1 != null) btnStation1.onClick.AddListener(() => FocusStationAndCamera(1, "ST-01"));
            if (btnStation2 != null) btnStation2.onClick.AddListener(() => FocusStationAndCamera(2, "ST-02"));
            if (btnStation3 != null) btnStation3.onClick.AddListener(() => FocusStationAndCamera(3, "ST-03"));
            if (btnStation4 != null) btnStation4.onClick.AddListener(() => FocusStationAndCamera(4, "ST-04"));

            if (btnInjectOverheat != null) btnInjectOverheat.onClick.AddListener(() => TelemetrySimulatorEngine.Instance?.ToggleFault1(currentSelectedStationId));
            if (btnInjectTorqueOverload != null) btnInjectTorqueOverload.onClick.AddListener(() => TelemetrySimulatorEngine.Instance?.ToggleFault2(currentSelectedStationId));
            if (btnInjectConveyorJam != null) btnInjectConveyorJam.onClick.AddListener(() => TelemetrySimulatorEngine.Instance?.ToggleFault3(currentSelectedStationId));
            if (btnEmergencyStop != null) btnEmergencyStop.onClick.AddListener(() => TelemetrySimulatorEngine.Instance?.ToggleEmergencyStop());
            if (btnResetFaults != null) btnResetFaults.onClick.AddListener(() => TelemetrySimulatorEngine.Instance?.ResetAllFaults());

            if (btnClosePartInspection != null) btnClosePartInspection.onClick.AddListener(() => partInspectionPanel?.SetActive(false));
        }

        private void Update()
        {
            if (clockText != null)
            {
                clockText.text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public void SelectStation(string stationId)
        {
            currentSelectedStationId = stationId;
            var st = TelemetrySimulatorEngine.Instance?.GetStation(stationId);
            if (st != null)
            {
                UpdateStationUI(st);
            }
            UpdateFaultButtonVisuals();
        }

        private void FocusStationAndCamera(int presetIndex, string stationId)
        {
            SelectStation(stationId);
            DigitalTwinCameraController.Instance?.FocusPreset(presetIndex);
        }

        private void HandleStationUpdated(StationTelemetryData data)
        {
            UpdateFacilityGlobalStatus();

            if (data.stationId == currentSelectedStationId)
            {
                UpdateStationUI(data);

                if (waveformChart != null)
                {
                    waveformChart.PushTelemetryValues(data.jointTorqueVector);
                }
            }
        }

        private void UpdateStationUI(StationTelemetryData data)
        {
            if (stationTitleText != null) stationTitleText.text = $"{data.stationName} <size=12><color=#7FA5C5>({data.stationId})</color></size>";

            // Machine State Badge
            if (machineStateBadgeText != null)
            {
                machineStateBadgeText.text = data.currentState.ToString().ToUpper();
                Color col = new Color(0.1f, 0.9f, 0.4f);
                if (data.currentState == MachineState.Warning) col = new Color(1.0f, 0.75f, 0.1f);
                if (data.currentState == MachineState.CriticalFault) col = new Color(1.0f, 0.2f, 0.2f);
                if (data.currentState == MachineState.Idle) col = new Color(0.7f, 0.7f, 0.7f);

                machineStateBadgeText.color = col;
                if (machineStateBadgeBg != null) machineStateBadgeBg.color = new Color(col.r, col.g, col.b, 0.2f);
            }

            // OEE
            if (oeeValueText != null) oeeValueText.text = $"{data.overallOEE:F1}%";
            if (oeeProgressBar != null) oeeProgressBar.fillAmount = Mathf.Clamp01(data.overallOEE / 100f);
            if (availabilityText != null) availabilityText.text = $"Avail: {data.availability:F1}%";
            if (performanceText != null) performanceText.text = $"Perf: {data.performance:F1}%";
            if (qualityText != null) qualityText.text = $"Qual: {data.quality:F1}%";

            // Kinematics & Velocity
            if (jointVelocityText != null)
            {
                if (data.stationType == StationType.ConveyorSorting)
                {
                    jointVelocityText.text = $"{data.conveyorSpeedMps:F2} <size=13>m/s</size>";
                    if (velocityRadialFill != null) velocityRadialFill.fillAmount = Mathf.Clamp01(data.conveyorSpeedMps / 2.5f);
                }
                else
                {
                    jointVelocityText.text = $"{data.jointVelocityDegPerSec:F0} <size=13>°/s</size>";
                    if (velocityRadialFill != null) velocityRadialFill.fillAmount = Mathf.Clamp01(data.jointVelocityDegPerSec / 250f);
                }
            }

            if (motorTorqueText != null) motorTorqueText.text = $"Torque: {data.motorTorqueNm:F1} Nm";
            if (cycleTimeText != null) cycleTimeText.text = $"Cycle: {data.cycleTimeSeconds:F1}s";

            // Joint Temperature
            if (tempValueText != null)
            {
                tempValueText.text = $"{data.jointTemperature:F1}°C";
                Color tCol = new Color(0.3f, 0.85f, 1f);
                if (data.jointTemperature > 58f) tCol = new Color(1f, 0.75f, 0.1f);
                if (data.jointTemperature > 72f) tCol = new Color(1f, 0.2f, 0.2f);
                tempValueText.color = tCol;
            }

            if (tempProgressBar != null) tempProgressBar.fillAmount = Mathf.InverseLerp(20f, 85f, data.jointTemperature);

            // Diagnostics Grid
            if (payloadText != null)
            {
                if (data.stationType == StationType.ConveyorSorting) payloadText.text = $"Vision Pass: {data.visionPassRate:F1}%";
                else payloadText.text = $"Payload: {data.endEffectorPayloadKg:F1} kg";
            }

            if (currentAmpsText != null) currentAmpsText.text = $"Current: {data.motorCurrentAmps:F1} A";
            if (toolWearText != null) toolWearText.text = $"Wear: {data.toolWearPercentage:F1}%";
            if (partsCounterText != null) partsCounterText.text = $"Units: {data.partsAssembled} / {data.partsTarget}";

            if (torqueMagnitudeText != null)
            {
                torqueMagnitudeText.text = $"{data.motorTorqueNm:F1} Nm";
                torqueMagnitudeText.color = data.motorTorqueNm > 90f ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.9f, 0.5f);
            }
        }

        public void UpdateFaultButtonVisuals()
        {
            var engine = TelemetrySimulatorEngine.Instance;
            if (engine == null) return;

            var st = engine.GetStation(currentSelectedStationId);
            bool f1 = engine.IsFault1Active(currentSelectedStationId);
            bool f2 = engine.IsFault2Active(currentSelectedStationId);
            bool f3 = engine.IsFault3Active(currentSelectedStationId);
            bool eStop = engine.IsEmergencyStopped;

            // Machine-specific labels
            string labelF1 = "🔥 Simulate Overheat";
            string labelF2 = "⚡ Inject Torque Overload";
            string labelF3 = "🦾 Tooling / Vision Fault";

            if (st != null)
            {
                switch (st.stationType)
                {
                    case StationType.RoboticAssembly:
                        labelF1 = f1 ? "⚠️ [ACTIVE] J3 Thermal Overheat" : "🔥 Simulate J3 Servo Overheat";
                        labelF2 = f2 ? "🛑 [ACTIVE] Torque Overload" : "⚡ Inject Torque Overload (>180 Nm)";
                        labelF3 = f3 ? "⚠️ [ACTIVE] Gripper Vacuum Loss" : "🦾 Gripper Vacuum Loss";
                        break;
                    case StationType.RoboticWelding:
                        labelF1 = f1 ? "⚠️ [ACTIVE] Torch Tip Overheat" : "🔥 Simulate Torch Thermal Overheat";
                        labelF2 = f2 ? "🛑 [ACTIVE] Arc Current Surge" : "⚡ Inject Arc Current Surge";
                        labelF3 = f3 ? "⚠️ [ACTIVE] Weld Tip Spatter / Jam" : "💥 Simulate Weld Tip Jam";
                        break;
                    case StationType.ConveyorSorting:
                        labelF1 = f1 ? "⚠️ [ACTIVE] Infeed Pallet Jam" : "📦 Trigger Infeed Pallet Jam";
                        labelF2 = f2 ? "🛑 [ACTIVE] Drive Belt Slippage" : "⚙️ Inject Belt Slippage";
                        labelF3 = f3 ? "⚠️ [ACTIVE] Vision Defect Alert" : "👁️ Optical Vision Defect";
                        break;
                    case StationType.FacilityPLC:
                        labelF1 = f1 ? "⚠️ [ACTIVE] Voltage Surge" : "⚡ Grid Voltage Surge (+45 kW)";
                        labelF2 = f2 ? "🛑 [ACTIVE] Network Packet Loss" : "🌐 Ethernet Packet Loss";
                        labelF3 = f3 ? "⚠️ [ACTIVE] Safety Curtain Trip" : "🛑 Safety Curtain Loop Trip";
                        break;
                }
            }

            SetButtonState(btnInjectOverheat, labelF1, f1, btnWarningActiveColor);
            SetButtonState(btnInjectTorqueOverload, labelF2, f2, btnCriticalActiveColor);
            SetButtonState(btnInjectConveyorJam, labelF3, f3, btnWarningActiveColor);

            // Emergency Stop Button
            if (btnEmergencyStop != null)
            {
                string eStopLabel = eStop ? "🛑 [ACTIVE] EMERGENCY STOP (PAUSED)" : "🛑 EMERGENCY STOP (Safety Curtain)";
                SetButtonState(btnEmergencyStop, eStopLabel, eStop, new Color(0.9f, 0.1f, 0.1f, 0.95f));
            }
        }

        private void SetButtonState(Button btn, string label, bool isActive, Color activeColor)
        {
            if (btn == null) return;

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = label;
                txt.color = isActive ? Color.white : new Color(0.85f, 0.95f, 1f);
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? activeColor : btnNormalColor;
            }
        }

        private void UpdateFacilityGlobalStatus()
        {
            if (facilityStatusText == null) return;

            bool isAnyCritical = false;
            bool isAnyWarn = false;

            var stations = TelemetrySimulatorEngine.Instance?.Stations;
            if (stations != null)
            {
                foreach (var s in stations)
                {
                    if (s.currentState == MachineState.CriticalFault) isAnyCritical = true;
                    else if (s.currentState == MachineState.Warning) isAnyWarn = true;
                }
            }

            if (isAnyCritical || (TelemetrySimulatorEngine.Instance != null && TelemetrySimulatorEngine.Instance.IsEmergencyStopped))
            {
                facilityStatusText.text = "ROBOTICS FLEET: CRITICAL ALARM";
                facilityStatusText.color = new Color(1.0f, 0.2f, 0.2f);
                if (facilityStatusDot != null) facilityStatusDot.color = new Color(1.0f, 0.2f, 0.2f);
            }
            else if (isAnyWarn)
            {
                facilityStatusText.text = "ROBOTICS FLEET: KINEMATIC WARNING";
                facilityStatusText.color = new Color(1.0f, 0.75f, 0.1f);
                if (facilityStatusDot != null) facilityStatusDot.color = new Color(1.0f, 0.75f, 0.1f);
            }
            else
            {
                facilityStatusText.text = "ROBOTICS FLEET: NOMINAL (AUTONOMOUS)";
                facilityStatusText.color = new Color(0.2f, 0.95f, 0.4f);
                if (facilityStatusDot != null) facilityStatusDot.color = new Color(0.2f, 0.95f, 0.4f);
            }
        }

        private void HandleAlarmRaised(AlarmEvent alarm)
        {
            AddLogEntry(alarm.stationId, alarm.message, alarm.severity);
        }

        private void AddLogEntry(string source, string message, AlarmSeverity severity)
        {
            string colorTag = "#7FA5C5";
            if (severity == AlarmSeverity.Warning) colorTag = "#FFAA00";
            if (severity == AlarmSeverity.Critical) colorTag = "#FF3333";

            string entry = $"<color=#6488A8>[{DateTime.Now:HH:mm:ss}]</color> <color={colorTag}><b>[{source}]</b> {message}</color>";
            alarmLogs.Add(entry);

            if (alarmLogs.Count > maxLogLines)
            {
                alarmLogs.RemoveAt(0);
            }

            if (alarmLogText != null)
            {
                alarmLogText.text = string.Join("\n", alarmLogs);
            }
        }

        private void HandlePartInspected(PartInspectableRaycaster part)
        {
            if (partInspectionPanel == null) return;

            partInspectionPanel.SetActive(true);
            if (partNameText != null) partNameText.text = part.PartName;
            if (partNumberText != null) partNumberText.text = $"Part #: {part.PartNumber}";
            if (partManufacturerText != null) partManufacturerText.text = $"OEM: {part.Manufacturer}";
            if (partHoursText != null) partHoursText.text = $"Operating Hours: {part.OperatingHours:N0} hrs";
            if (partHealthText != null)
            {
                partHealthText.text = $"Health Score: {part.HealthScore:F1}%";
                partHealthText.color = part.HealthScore > 85f ? new Color(0.2f, 0.9f, 0.4f) : new Color(1.0f, 0.7f, 0.1f);
            }
        }
    }
}
