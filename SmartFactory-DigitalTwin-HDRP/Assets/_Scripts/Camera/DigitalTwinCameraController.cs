using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SmartFactory.DigitalTwin.Navigation
{
    [Serializable]
    public class CameraPreset
    {
        public string presetName;
        public Vector3 pivotPoint;
        public float distance = 5.0f;
        public float yaw = 25f;
        public float pitch = 14f;
    }

    public class DigitalTwinCameraController : MonoBehaviour
    {
        public static DigitalTwinCameraController Instance { get; private set; }

        [Header("Orbit & Navigation Speeds")]
        [SerializeField] private float orbitSpeed = 3.5f;
        [SerializeField] private float zoomSpeed = 5.0f;
        [SerializeField] private float panSpeed = 0.5f;
        [SerializeField] private float damping = 9.0f;

        [Header("Distance Limits")]
        [SerializeField] private float minDistance = 1.8f;
        [SerializeField] private float maxDistance = 14.0f;

        [Header("Pitch Limits")]
        [SerializeField] private float minPitch = 4.0f;
        [SerializeField] private float maxPitch = 35.0f;

        [Header("Height Limits")]
        [SerializeField] private float maxCameraHeight = 3.6f;
        [SerializeField] private float minCameraHeight = 0.8f;

        [Header("Presets")]
        [SerializeField] private List<CameraPreset> presets = new List<CameraPreset>();
        [SerializeField] private float transitionDuration = 0.85f;

        [Header("Current State")]
        [SerializeField] private Vector3 currentPivot;
        [SerializeField] private float currentDistance = 6.0f;
        [SerializeField] private float currentYaw = 25.0f;
        [SerializeField] private float currentPitch = 14.0f;

        private Vector3 targetPivot;
        private float targetDistance;
        private float targetYaw;
        private float targetPitch;

        private bool isTransitioning = false;
        private float transitionProgress = 0f;
        private Vector3 startPivot;
        private float startDistance, startYaw, startPitch;

        public int CurrentPresetIndex { get; private set; } = 0;
        public IReadOnlyList<CameraPreset> Presets => presets;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeFactoryPresets();
        }

        private void Start()
        {
            targetPivot = currentPivot;
            targetDistance = currentDistance;
            targetYaw = currentYaw;
            targetPitch = currentPitch;

            ApplyInstantTransform();
        }

        public void InitializeFactoryPresets()
        {
            presets.Clear();

            // Preset 0: Factory Overview (Hero Main Aisle Perspective)
            presets.Add(new CameraPreset
            {
                presetName = "Facility Overview",
                pivotPoint = new Vector3(22.0f, 1.2f, 26.0f),
                distance = 7.5f,
                yaw = 35.0f,
                pitch = 16.0f
            });

            // Preset 1: Station 1 - 6-Axis Robot Assembly Cell 1 (Clean Aisle View into Arm)
            presets.Add(new CameraPreset
            {
                presetName = "Robot Cell 1: Assembly",
                pivotPoint = new Vector3(20.5f, 1.1f, 25.0f),
                distance = 4.6f,
                yaw = 15.0f,
                pitch = 14.0f
            });

            // Preset 2: Station 2 - Robot Welding & Fastening (Open Aisle View into Welding Cell)
            presets.Add(new CameraPreset
            {
                presetName = "Robot Cell 2: Welding",
                pivotPoint = new Vector3(28.5f, 1.1f, 42.0f),
                distance = 4.8f,
                yaw = -50.0f,
                pitch = 14.0f
            });

            // Preset 3: Station 3 - Conveyor & Vision Line (Along Conveyor Flow)
            presets.Add(new CameraPreset
            {
                presetName = "Conveyor & Vision Line",
                pivotPoint = new Vector3(22.0f, 0.9f, 28.0f),
                distance = 5.0f,
                yaw = 75.0f,
                pitch = 14.0f
            });

            // Preset 4: Station 4 - Central PLC & Safety Substation (Direct HMI View)
            presets.Add(new CameraPreset
            {
                presetName = "Central PLC & Safety",
                pivotPoint = new Vector3(12.8f, 1.1f, 17.8f),
                distance = 4.0f,
                yaw = 25.0f,
                pitch = 12.0f
            });

            currentPivot = presets[0].pivotPoint;
            currentDistance = presets[0].distance;
            currentYaw = presets[0].yaw;
            currentPitch = presets[0].pitch;
        }

        public void RebindToActualSceneObjects()
        {
            var visualizers = FindObjectsByType<Visuals.StationVisualizer>(FindObjectsSortMode.None);
            foreach (var vis in visualizers)
            {
                Vector3 p = vis.transform.position;
                p.y = Mathf.Clamp(p.y + 1.1f, 0.9f, 2.0f);

                if (vis.StationId == "ST-01" && presets.Count > 1)
                {
                    presets[1].pivotPoint = p;
                }
                else if (vis.StationId == "ST-02" && presets.Count > 2)
                {
                    presets[2].pivotPoint = p;
                }
                else if (vis.StationId == "ST-03" && presets.Count > 3)
                {
                    presets[3].pivotPoint = p;
                }
                else if (vis.StationId == "ST-04" && presets.Count > 4)
                {
                    presets[4].pivotPoint = p;
                }
            }

            if (presets.Count > 4)
            {
                Vector3 mid = (presets[1].pivotPoint + presets[2].pivotPoint + presets[3].pivotPoint + presets[4].pivotPoint) * 0.25f;
                mid.y = 1.2f;
                presets[0].pivotPoint = mid;
            }
        }

        private void Update()
        {
            HandleKeyboardShortcuts();

            if (isTransitioning)
            {
                UpdatePresetTransition();
            }
            else
            {
                HandleUserMouseInput();
            }

            ApplyDampedTransform();
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) FocusPreset(0);
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) FocusPreset(1);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) FocusPreset(2);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) FocusPreset(3);
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) FocusPreset(4);
        }

        private void HandleUserMouseInput()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue();

            // 1. Orbit (Right Mouse Button Drag)
            if (mouse.rightButton.isPressed)
            {
                targetYaw += mouseDelta.x * orbitSpeed * 0.08f;
                targetPitch -= mouseDelta.y * orbitSpeed * 0.08f;
                targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            }

            // 2. Pan (Middle Mouse Button or Alt + Left Click)
            var kb = Keyboard.current;
            bool isAltPressed = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);

            if (mouse.middleButton.isPressed || (isAltPressed && mouse.leftButton.isPressed))
            {
                Quaternion rot = Quaternion.Euler(targetPitch, targetYaw, 0f);
                Vector3 right = rot * Vector3.right;
                Vector3 up = rot * Vector3.up;

                float factor = (targetDistance / 10f) * panSpeed * 0.015f;
                targetPivot -= right * mouseDelta.x * factor;
                targetPivot -= up * mouseDelta.y * factor;

                targetPivot.y = Mathf.Clamp(targetPivot.y, 0.8f, 2.2f);
            }

            // 3. Zoom (Scroll Wheel)
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                float scrollNormalized = Mathf.Sign(scrollY) * Mathf.Clamp01(Mathf.Abs(scrollY) / 120f);
                targetDistance -= scrollNormalized * zoomSpeed * (targetDistance * 0.12f);
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        public void FocusPreset(int index)
        {
            if (index < 0 || index >= presets.Count) return;

            CurrentPresetIndex = index;
            var preset = presets[index];

            startPivot = currentPivot;
            startDistance = currentDistance;
            startYaw = currentYaw;
            startPitch = currentPitch;

            targetPivot = preset.pivotPoint;
            targetDistance = preset.distance;
            targetYaw = preset.yaw;
            targetPitch = preset.pitch;

            isTransitioning = true;
            transitionProgress = 0f;
        }

        public void FocusTarget(Vector3 worldPosition, float distance = 4.8f, float pitch = 14f, float yaw = 25f)
        {
            startPivot = currentPivot;
            startDistance = currentDistance;
            startYaw = currentYaw;
            startPitch = currentPitch;

            targetPivot = worldPosition;
            targetPivot.y = Mathf.Clamp(targetPivot.y, 0.8f, 2.0f);
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetYaw = yaw;

            isTransitioning = true;
            transitionProgress = 0f;
        }

        private void UpdatePresetTransition()
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            float t = SmoothStep(Mathf.Clamp01(transitionProgress));

            currentPivot = Vector3.Lerp(startPivot, targetPivot, t);
            currentDistance = Mathf.Lerp(startDistance, targetDistance, t);
            currentYaw = Mathf.LerpAngle(startYaw, targetYaw, t);
            currentPitch = Mathf.Lerp(startPitch, targetPitch, t);

            if (transitionProgress >= 1f)
            {
                isTransitioning = false;
                targetPivot = currentPivot;
                targetDistance = currentDistance;
                targetYaw = currentYaw;
                targetPitch = currentPitch;
            }
        }

        private void ApplyDampedTransform()
        {
            if (!isTransitioning)
            {
                float d = Time.deltaTime * damping;
                currentPivot = Vector3.Lerp(currentPivot, targetPivot, d);
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, d);
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, d);
                currentPitch = Mathf.Lerp(currentPitch, targetPitch, d);
            }

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 position = currentPivot - (rotation * Vector3.forward * currentDistance);

            position.y = Mathf.Clamp(position.y, minCameraHeight, maxCameraHeight);

            transform.position = position;
            transform.rotation = rotation;
        }

        private void ApplyInstantTransform()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 position = currentPivot - (rotation * Vector3.forward * currentDistance);
            position.y = Mathf.Clamp(position.y, minCameraHeight, maxCameraHeight);
            transform.position = position;
            transform.rotation = rotation;
        }

        private float SmoothStep(float x)
        {
            return x * x * (3f - 2f * x);
        }

        public void SetPresetValues(int index, Vector3 pivot, float distance, float yaw, float pitch)
        {
            if (index >= 0 && index < presets.Count)
            {
                presets[index].pivotPoint = pivot;
                presets[index].distance = distance;
                presets[index].yaw = yaw;
                presets[index].pitch = pitch;
            }
        }
    }
}
