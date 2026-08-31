using System;
using UnityEngine;
using UnityEngine.EventSystems;
using SmartFactory.DigitalTwin.Data;
using SmartFactory.DigitalTwin.Navigation;

namespace SmartFactory.DigitalTwin.Visuals
{
    public class StationVisualizer : MonoBehaviour, IPointerClickHandler
    {
        [Header("Station Configuration")]
        [SerializeField] private string stationId = "ST-01";
        [SerializeField] private string stationDisplayName = "6-Axis Robot Assembly Cell 1";
        [SerializeField] private StationType stationType = StationType.RoboticAssembly;

        [Header("Kinematics / Moving Joints")]
        [SerializeField] private Transform rotatingJoint;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Header("Andon Light Renderers")]
        [SerializeField] private Renderer greenLightRenderer;
        [SerializeField] private Renderer amberLightRenderer;
        [SerializeField] private Renderer redLightRenderer;

        [Header("Thermal & Stress Highlighting")]
        [SerializeField] private Renderer thermalMeshRenderer;
        [SerializeField] private Color normalColor = new Color(0.1f, 0.5f, 0.9f, 0.1f);
        [SerializeField] private Color warningColor = new Color(1.0f, 0.6f, 0.0f, 0.6f);
        [SerializeField] private Color criticalColor = new Color(1.0f, 0.1f, 0.1f, 0.9f);

        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private StationTelemetryData currentTelemetry;
        private bool isFlashing = false;
        private float flashTimer = 0f;

        public string StationId => stationId;
        public string DisplayName => stationDisplayName;
        public StationType Type => stationType;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            EnsureClickCollider();
        }

        private void Start()
        {
            if (TelemetrySimulatorEngine.Instance != null)
            {
                TelemetrySimulatorEngine.Instance.OnStationTelemetryUpdated += HandleTelemetryUpdated;
            }
        }

        private void OnDestroy()
        {
            if (TelemetrySimulatorEngine.Instance != null)
            {
                TelemetrySimulatorEngine.Instance.OnStationTelemetryUpdated -= HandleTelemetryUpdated;
            }
        }

        private void EnsureClickCollider()
        {
            if (GetComponent<Collider>() == null)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(2.5f, 2.0f, 2.5f);
                col.center = new Vector3(0f, 1.0f, 0f);
            }
        }

        private void Update()
        {
            // Animate joint rotation if assigned
            if (rotatingJoint != null && currentTelemetry != null && currentTelemetry.jointVelocityDegPerSec > 0)
            {
                float angle = currentTelemetry.jointVelocityDegPerSec * Time.deltaTime;
                rotatingJoint.Rotate(rotationAxis, angle, Space.Self);
            }

            // Flash Andon light if in Warning / Critical state
            if (isFlashing)
            {
                flashTimer += Time.deltaTime;
                bool flashOn = (Mathf.FloorToInt(flashTimer * 4f) % 2) == 0;
                UpdateAndonVisuals(flashOn);
            }
        }

        private void HandleTelemetryUpdated(StationTelemetryData data)
        {
            if (data.stationId != stationId) return;

            currentTelemetry = data;
            isFlashing = (data.currentState == MachineState.Warning || data.currentState == MachineState.CriticalFault);

            UpdateAndonVisuals(true);
            UpdateThermalVisuals(data.jointTemperature);
        }

        private void UpdateAndonVisuals(bool activeState)
        {
            Color green = Color.black;
            Color amber = Color.black;
            Color red = Color.black;

            if (currentTelemetry != null)
            {
                switch (currentTelemetry.currentState)
                {
                    case MachineState.Running:
                        green = new Color(0.1f, 1.0f, 0.2f) * 3.5f;
                        break;
                    case MachineState.Warning:
                        amber = activeState ? (new Color(1.0f, 0.7f, 0.0f) * 4.0f) : Color.black;
                        break;
                    case MachineState.CriticalFault:
                        red = activeState ? (new Color(1.0f, 0.1f, 0.1f) * 5.0f) : Color.black;
                        break;
                    case MachineState.Idle:
                        amber = new Color(0.8f, 0.8f, 0.2f) * 1.5f;
                        break;
                }
            }

            SetRendererEmission(greenLightRenderer, green);
            SetRendererEmission(amberLightRenderer, amber);
            SetRendererEmission(redLightRenderer, red);
        }

        private void UpdateThermalVisuals(float temperature)
        {
            if (thermalMeshRenderer == null) return;

            float normTemp = Mathf.InverseLerp(40f, 85f, temperature);
            Color targetColor = Color.Lerp(normalColor, warningColor, normTemp * 1.5f);
            if (normTemp > 0.6f)
            {
                targetColor = Color.Lerp(warningColor, criticalColor, (normTemp - 0.6f) / 0.4f);
            }

            thermalMeshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorId, targetColor * (normTemp * 2.5f));
            thermalMeshRenderer.SetPropertyBlock(propBlock);
        }

        private void SetRendererEmission(Renderer rend, Color emissionColor)
        {
            if (rend == null) return;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorId, emissionColor);
            rend.SetPropertyBlock(propBlock);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (DigitalTwinCameraController.Instance != null)
            {
                DigitalTwinCameraController.Instance.FocusTarget(transform.position + Vector3.up * 1.0f, 4.8f, 14f, 30f);
            }
        }
    }
}
