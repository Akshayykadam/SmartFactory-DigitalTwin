using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartFactory.DigitalTwin.Data
{
    public class TelemetrySimulatorEngine : MonoBehaviour
    {
        public static TelemetrySimulatorEngine Instance { get; private set; }

        public event Action<StationTelemetryData> OnStationTelemetryUpdated;
        public event Action<AlarmEvent> OnAlarmRaised;
        public event Action OnGlobalTelemetryTick;
        public event Action OnFaultsChanged;

        [Header("Simulation Settings")]
        [SerializeField] private float updateInterval = 0.05f; // 20 Hz tick
        [SerializeField] private bool autoSimulate = true;

        [Header("Active Robotic Stations")]
        [SerializeField] private List<StationTelemetryData> stations = new List<StationTelemetryData>();

        // Machine-specific fault slots per station
        private Dictionary<string, bool> fault1Map = new Dictionary<string, bool>(); // Primary Thermal / Jam
        private Dictionary<string, bool> fault2Map = new Dictionary<string, bool>(); // Torque Overload / Voltage
        private Dictionary<string, bool> fault3Map = new Dictionary<string, bool>(); // Tool Loss / Optical Defect
        private bool isEmergencyStopActive = false;

        // Kinematic cycle state per station
        private float[] cycleTimers = new float[4];
        private float[] cycleDurations = new float[] { 7.0f, 8.5f, 3.0f, 10.0f };
        private int[] cycleCounters = new int[4];

        // Smooth physics memory
        private float[] currentJointVelocities = new float[4];
        private float[] currentTorques = new float[4];
        private float[] currentTemperatures = new float[] { 44.5f, 49.2f, 33.8f, 28.2f };

        private float nextUpdateTime;

        public IReadOnlyList<StationTelemetryData> Stations => stations;
        public bool IsEmergencyStopped => isEmergencyStopActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeRoboticStations();
        }

        private void InitializeRoboticStations()
        {
            stations.Clear();

            // Station 1: 6-Axis Robot Assembly Cell 1
            var robot1 = new StationTelemetryData("ST-01", "6-Axis Robot Assembly Cell 1", StationType.RoboticAssembly)
            {
                jointVelocityDegPerSec = 0f,
                motorTorqueNm = 42f,
                endEffectorPayloadKg = 14.5f,
                jointTemperature = 45.2f,
                motorCurrentAmps = 10.8f,
                cycleTimeSeconds = 7.0f,
                partsAssembled = 482,
                partsTarget = 600,
                trajectoryAccuracyMm = 0.018f,
                availability = 98.4f,
                performance = 96.5f,
                quality = 99.6f
            };
            robot1.RecalculateOEE();
            stations.Add(robot1);

            // Station 2: Precision Robotic Welding & Fastening
            var robot2 = new StationTelemetryData("ST-02", "Robotic Welding & Fastening", StationType.RoboticWelding)
            {
                jointVelocityDegPerSec = 0f,
                motorTorqueNm = 58f,
                endEffectorPayloadKg = 8.0f,
                jointTemperature = 49.5f,
                motorCurrentAmps = 14.2f,
                cycleTimeSeconds = 8.5f,
                partsAssembled = 394,
                partsTarget = 500,
                trajectoryAccuracyMm = 0.012f,
                availability = 97.8f,
                performance = 95.2f,
                quality = 99.8f
            };
            robot2.RecalculateOEE();
            stations.Add(robot2);

            // Station 3: High-Speed Conveyor & Vision Sorting
            var conveyor = new StationTelemetryData("ST-03", "Conveyor & Vision Sorting Line", StationType.ConveyorSorting)
            {
                conveyorSpeedMps = 1.25f,
                motorTorqueNm = 24.5f,
                jointTemperature = 33.8f,
                motorCurrentAmps = 5.6f,
                visionPassRate = 99.6f,
                cycleTimeSeconds = 2.8f,
                partsAssembled = 1240,
                partsTarget = 1500,
                availability = 99.2f,
                performance = 98.6f,
                quality = 99.6f
            };
            conveyor.RecalculateOEE();
            stations.Add(conveyor);

            // Station 4: Plant Central PLC & Safety Substation
            var power = new StationTelemetryData("ST-04", "Central PLC & Safety Substation", StationType.FacilityPLC)
            {
                powerConsumptionKw = 88.5f,
                motorCurrentAmps = 52.0f,
                jointTemperature = 28.2f,
                availability = 99.9f,
                performance = 99.4f,
                quality = 100.0f
            };
            power.RecalculateOEE();
            stations.Add(power);

            foreach (var st in stations)
            {
                fault1Map[st.stationId] = false;
                fault2Map[st.stationId] = false;
                fault3Map[st.stationId] = false;
            }
        }

        private void Update()
        {
            if (!autoSimulate) return;

            if (Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;
                SimulateRealisticIndustrialPhysics(updateInterval);
            }
        }

        private void SimulateRealisticIndustrialPhysics(float dt)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                var st = stations[i];
                bool f1 = fault1Map.TryGetValue(st.stationId, out bool val1) && val1;
                bool f2 = fault2Map.TryGetValue(st.stationId, out bool val2) && val2;
                bool f3 = fault3Map.TryGetValue(st.stationId, out bool val3) && val3;

                if (isEmergencyStopActive)
                {
                    st.currentState = MachineState.CriticalFault;
                    currentJointVelocities[i] = Mathf.Lerp(currentJointVelocities[i], 0f, dt * 10f);
                    currentTorques[i] = Mathf.Lerp(currentTorques[i], 0f, dt * 10f);
                    st.jointVelocityDegPerSec = currentJointVelocities[i];
                    st.motorTorqueNm = currentTorques[i];
                    st.conveyorSpeedMps = 0f;
                    st.jointTorqueVector = Vector3.Lerp(st.jointTorqueVector, Vector3.zero, dt * 10f);
                    st.availability = Mathf.Max(60f, st.availability - dt * 2f);
                    st.RecalculateOEE();
                    OnStationTelemetryUpdated?.Invoke(st);
                    continue;
                }

                // Advance cycle timer
                cycleTimers[i] += dt;
                float cycleDuration = cycleDurations[i];
                float progress = (cycleTimers[i] % cycleDuration) / cycleDuration;

                int currentCycleNum = Mathf.FloorToInt(cycleTimers[i] / cycleDuration);
                if (currentCycleNum > cycleCounters[i] && !f1 && !f2 && !f3)
                {
                    cycleCounters[i] = currentCycleNum;
                    st.partsAssembled++;
                }

                // 1. Robotics Workcells (ST-01 Assembly & ST-02 Welding)
                if (st.stationType == StationType.RoboticAssembly || st.stationType == StationType.RoboticWelding)
                {
                    float maxVel = (st.stationType == StationType.RoboticAssembly) ? 175f : 130f;
                    float targetVel = 0f;
                    float targetTorqueJ2 = 25f;

                    if (progress < 0.18f)
                    {
                        targetVel = 0f;
                        targetTorqueJ2 = 28f;
                    }
                    else if (progress < 0.38f)
                    {
                        float t = (progress - 0.18f) / 0.20f;
                        float sCurve = t * t * (3f - 2f * t);
                        targetVel = sCurve * maxVel;
                        targetTorqueJ2 = Mathf.Lerp(30f, 68f, Mathf.Sin(t * Mathf.PI));
                    }
                    else if (progress < 0.65f)
                    {
                        targetVel = maxVel;
                        targetTorqueJ2 = 36f + (Mathf.PerlinNoise(Time.time * 2f, i) - 0.5f) * 4f;
                    }
                    else if (progress < 0.85f)
                    {
                        float t = (progress - 0.65f) / 0.20f;
                        float sCurve = 1f - (t * t * (3f - 2f * t));
                        targetVel = sCurve * maxVel;
                        targetTorqueJ2 = Mathf.Lerp(36f, 52f, Mathf.Sin(t * Mathf.PI));
                    }
                    else
                    {
                        targetVel = 0f;
                        targetTorqueJ2 = 22f;
                    }

                    // Machine-specific fault overrides
                    if (f2) // Torque Overload / Collision (ST-01 or ST-02)
                    {
                        targetTorqueJ2 = 185f;
                        targetVel = Mathf.Min(targetVel, 15f); // Robot stalls under mechanical overload
                    }

                    if (f3 && st.stationType == StationType.RoboticAssembly) // Gripper Vacuum Loss
                    {
                        st.endEffectorPayloadKg = 0.2f; // Payload lost/dropped
                    }
                    else if (f3 && st.stationType == StationType.RoboticWelding) // Weld Tip Spatter / Jam
                    {
                        st.motorCurrentAmps = 28.5f; // Current surge
                        targetTorqueJ2 = 92f;
                    }

                    currentJointVelocities[i] = Mathf.Lerp(currentJointVelocities[i], targetVel, dt * 12f);
                    currentTorques[i] = Mathf.Lerp(currentTorques[i], targetTorqueJ2, dt * 8f);

                    st.jointVelocityDegPerSec = currentJointVelocities[i];
                    st.motorTorqueNm = currentTorques[i];

                    float j1 = Mathf.Max(6f, currentTorques[i] * 0.75f + Mathf.Sin(Time.time * 8f) * 1.5f);
                    float j2 = currentTorques[i];
                    float j3 = Mathf.Max(5f, currentTorques[i] * 0.55f + Mathf.Cos(Time.time * 8f) * 1.2f);
                    st.jointTorqueVector = new Vector3(j1, j2, j3);

                    if (!f3)
                    {
                        st.motorCurrentAmps = 2.0f + (st.motorTorqueNm / 45f) * 8.5f;
                    }
                }
                // 2. Conveyor Workcell (ST-03)
                else if (st.stationType == StationType.ConveyorSorting)
                {
                    if (f1) // Pallet Jam
                    {
                        st.conveyorSpeedMps = Mathf.Lerp(st.conveyorSpeedMps, 0.02f, dt * 6f);
                        st.motorTorqueNm = Mathf.Lerp(st.motorTorqueNm, 78f, dt * 4f);
                    }
                    else if (f2) // Belt Slip
                    {
                        st.conveyorSpeedMps = Mathf.Lerp(st.conveyorSpeedMps, 0.45f, dt * 3f);
                        st.motorTorqueNm = Mathf.Lerp(st.motorTorqueNm, 48f, dt * 2f);
                    }
                    else
                    {
                        float beltNoise = (Mathf.PerlinNoise(Time.time * 1.5f, 0f) - 0.5f) * 0.03f;
                        st.conveyorSpeedMps = 1.25f + beltNoise;
                        st.motorTorqueNm = 24.5f + (Mathf.PerlinNoise(Time.time * 2f, 10f) - 0.5f) * 1.8f;
                    }

                    if (f3) // Optical Defect
                    {
                        st.visionPassRate = Mathf.Lerp(st.visionPassRate, 68.5f, dt * 3f);
                    }
                    else
                    {
                        st.visionPassRate = Mathf.Lerp(st.visionPassRate, 99.6f, dt * 0.5f);
                    }

                    st.jointTorqueVector = new Vector3(st.motorTorqueNm * 0.9f, st.motorTorqueNm, st.motorTorqueNm * 0.7f);
                    st.motorCurrentAmps = 3.0f + (st.motorTorqueNm / 25f) * 2.6f;
                }
                // 3. Facility PLC (ST-04)
                else if (st.stationType == StationType.FacilityPLC)
                {
                    float baseKw = 88.5f + (currentTorques[0] + currentTorques[1]) * 0.25f;
                    if (f1) baseKw += 45.0f; // Voltage surge
                    st.powerConsumptionKw = baseKw;
                    st.motorTorqueNm = 20.0f;
                    st.jointTorqueVector = new Vector3(18f, 20f, 15f);
                }

                // 4. Thermodynamics (Joule Heating & Fault 1 Overheat)
                float ambientTemp = 24.0f;
                float targetTemp = ambientTemp + (st.motorTorqueNm / 45f) * 22f;
                if (f1 && (st.stationType == StationType.RoboticAssembly || st.stationType == StationType.RoboticWelding))
                {
                    targetTemp = 82.5f; // Rapid thermal overheat spike
                }

                currentTemperatures[i] += (targetTemp - currentTemperatures[i]) * (dt * (f1 ? 0.6f : 0.15f));
                st.jointTemperature = currentTemperatures[i];

                // 5. Machine State & Alarms
                if (f2 || (f1 && st.jointTemperature >= 72f))
                {
                    st.currentState = MachineState.CriticalFault;
                    st.availability = Mathf.Max(75f, st.availability - dt * 1.5f);
                }
                else if (f1 || f3 || st.jointTemperature >= 58f)
                {
                    st.currentState = MachineState.Warning;
                    st.availability = Mathf.Max(88f, st.availability - dt * 0.5f);
                }
                else
                {
                    st.currentState = MachineState.Running;
                    st.availability = Mathf.Min(98.4f, st.availability + dt * 0.5f);
                }

                st.toolWearPercentage = Mathf.Min(100f, 16.0f + (st.partsAssembled * 0.001f));
                st.RecalculateOEE();
                OnStationTelemetryUpdated?.Invoke(st);
            }

            OnGlobalTelemetryTick?.Invoke();
        }

        public void RaiseAlarm(string stationId, string message, AlarmSeverity severity)
        {
            var alarm = new AlarmEvent(stationId, message, severity);
            OnAlarmRaised?.Invoke(alarm);
        }

        #region Machine-Specific Fault Injection API

        public bool IsFault1Active(string stationId) => fault1Map.TryGetValue(stationId, out bool v) && v;
        public bool IsFault2Active(string stationId) => fault2Map.TryGetValue(stationId, out bool v) && v;
        public bool IsFault3Active(string stationId) => fault3Map.TryGetValue(stationId, out bool v) && v;

        public void ToggleFault1(string stationId)
        {
            if (fault1Map.ContainsKey(stationId))
            {
                fault1Map[stationId] = !fault1Map[stationId];
                bool active = fault1Map[stationId];

                var st = GetStation(stationId);
                string faultDesc = (st?.stationType == StationType.ConveyorSorting) ? "Infeed Pallet Jam" :
                                   (st?.stationType == StationType.FacilityPLC) ? "Grid Voltage Surge" : "Servo Motor Thermal Overheat";

                if (active) RaiseAlarm(stationId, $"FAULT INJECTED: {faultDesc}", AlarmSeverity.Warning);
                else RaiseAlarm(stationId, $"FAULT CLEARED: {faultDesc}", AlarmSeverity.Info);

                OnFaultsChanged?.Invoke();
            }
        }

        public void ToggleFault2(string stationId)
        {
            if (fault2Map.ContainsKey(stationId))
            {
                fault2Map[stationId] = !fault2Map[stationId];
                bool active = fault2Map[stationId];

                var st = GetStation(stationId);
                string faultDesc = (st?.stationType == StationType.ConveyorSorting) ? "Belt Slippage & Speed Drop" :
                                   (st?.stationType == StationType.FacilityPLC) ? "Industrial Ethernet Packet Loss" : "Motor Drive Torque Overload (>180 Nm)";

                if (active) RaiseAlarm(stationId, $"CRITICAL FAULT: {faultDesc}", AlarmSeverity.Critical);
                else RaiseAlarm(stationId, $"FAULT CLEARED: {faultDesc}", AlarmSeverity.Info);

                OnFaultsChanged?.Invoke();
            }
        }

        public void ToggleFault3(string stationId)
        {
            if (fault3Map.ContainsKey(stationId))
            {
                fault3Map[stationId] = !fault3Map[stationId];
                bool active = fault3Map[stationId];

                var st = GetStation(stationId);
                string faultDesc = (st?.stationType == StationType.RoboticAssembly) ? "Gripper Vacuum Loss / Part Drop" :
                                   (st?.stationType == StationType.RoboticWelding) ? "Weld Tip Spatter & High Current" :
                                   (st?.stationType == StationType.ConveyorSorting) ? "Optical Vision Inspection Defect" : "Safety Light Curtain Trip";

                if (active) RaiseAlarm(stationId, $"FAULT INJECTED: {faultDesc}", AlarmSeverity.Warning);
                else RaiseAlarm(stationId, $"FAULT CLEARED: {faultDesc}", AlarmSeverity.Info);

                OnFaultsChanged?.Invoke();
            }
        }

        public void ToggleEmergencyStop()
        {
            isEmergencyStopActive = !isEmergencyStopActive;
            ApplyPhysicalEmergencyStop(isEmergencyStopActive);
            OnFaultsChanged?.Invoke();
        }

        private void ApplyPhysicalEmergencyStop(bool isStopped)
        {
            UnityFactorySceneHDRP.CustomSplineAnimate.IsEmergencyPaused = isStopped;

            var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                anim.speed = isStopped ? 0f : 1f;
            }

            var anims = FindObjectsByType<Animation>(FindObjectsSortMode.None);
            foreach (var a in anims)
            {
                if (isStopped) a.Stop();
                a.enabled = !isStopped;
            }

            if (isStopped)
            {
                RaiseAlarm("SAFETY", "SAFETY CURTAIN TRIPPED: PLANT-WIDE EMERGENCY STOP", AlarmSeverity.Critical);
            }
            else
            {
                RaiseAlarm("SAFETY", "Safety loop restored. Resuming robot kinematics.", AlarmSeverity.Info);
            }
        }

        public void ResetAllFaults()
        {
            isEmergencyStopActive = false;
            ApplyPhysicalEmergencyStop(false);

            var keys = new List<string>(fault1Map.Keys);
            foreach (var k in keys)
            {
                fault1Map[k] = false;
                fault2Map[k] = false;
                fault3Map[k] = false;
            }
            RaiseAlarm("SYSTEM", "All robotics simulated faults and safety overrides cleared.", AlarmSeverity.Info);
            OnFaultsChanged?.Invoke();
        }

        #endregion

        public StationTelemetryData GetStation(string stationId)
        {
            return stations.Find(s => s.stationId == stationId);
        }
    }
}
