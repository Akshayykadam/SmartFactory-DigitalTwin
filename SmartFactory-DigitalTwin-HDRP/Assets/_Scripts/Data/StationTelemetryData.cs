using System;
using UnityEngine;

namespace SmartFactory.DigitalTwin.Data
{
    public enum MachineState
    {
        Running,
        Idle,
        Warning,
        CriticalFault,
        Maintenance
    }

    public enum StationType
    {
        RoboticAssembly,
        RoboticWelding,
        ConveyorSorting,
        FacilityPLC
    }

    public enum AlarmSeverity
    {
        Info,
        Warning,
        Critical
    }

    [Serializable]
    public class AlarmEvent
    {
        public string timestamp;
        public string stationId;
        public string message;
        public AlarmSeverity severity;

        public AlarmEvent(string stationId, string message, AlarmSeverity severity)
        {
            this.timestamp = DateTime.Now.ToString("HH:mm:ss");
            this.stationId = stationId;
            this.message = message;
            this.severity = severity;
        }
    }

    [Serializable]
    public class StationTelemetryData
    {
        public string stationId;
        public string stationName;
        public StationType stationType;
        public MachineState currentState = MachineState.Running;

        // Robotics & Kinematics Telemetry
        public float jointVelocityDegPerSec; // Joint rotational velocity (deg/s)
        public float motorTorqueNm;          // Joint drive torque (Nm)
        public float endEffectorPayloadKg;   // Current payload mass (kg)
        public float cycleTimeSeconds;       // Robotic cycle time (s)
        public int partsAssembled;           // Total parts handled/assembled
        public int partsTarget;              // Shift target count
        public float trajectoryAccuracyMm;   // Precision deviation in mm (e.g. 0.02mm)

        // Sensors & Thermal Diagnostics
        public float jointTemperature;       // Joint/Servo temperature (°C) (Norm: 35-50, Warn: 65, Crit: 80)
        public float motorCurrentAmps;       // Motor current in Amperes
        public Vector3 jointTorqueVector;    // Real-time J1, J2, J3 dynamic load (Nm)
        public float toolWearPercentage;     // Gripper / Tool wear (0-100%)

        // Conveyor & Facility Specific
        public float conveyorSpeedMps;       // Conveyor speed (m/s)
        public float visionPassRate;         // Optical inspection pass rate % (e.g. 99.4%)
        public float powerConsumptionKw;     // Kilowatts

        // OEE (Overall Equipment Effectiveness)
        public float availability;           // 0 - 100%
        public float performance;            // 0 - 100%
        public float quality;                // 0 - 100%
        public float overallOEE;             // Availability * Performance * Quality

        public StationTelemetryData(string id, string name, StationType type)
        {
            stationId = id;
            stationName = name;
            stationType = type;

            jointVelocityDegPerSec = 145f;
            motorTorqueNm = 42.5f;
            endEffectorPayloadKg = 12.0f;
            cycleTimeSeconds = 6.8f;
            partsAssembled = 482;
            partsTarget = 600;
            trajectoryAccuracyMm = 0.03f;

            jointTemperature = 44.2f;
            motorCurrentAmps = 9.8f;
            jointTorqueVector = new Vector3(38f, 45f, 29f);
            toolWearPercentage = 16.2f;

            conveyorSpeedMps = 1.2f;
            visionPassRate = 99.6f;
            powerConsumptionKw = 18.5f;

            availability = 97.4f;
            performance = 94.8f;
            quality = 99.6f;
            RecalculateOEE();
        }

        public void RecalculateOEE()
        {
            overallOEE = (availability * performance * quality) / 10000f;
        }
    }
}
