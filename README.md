# Smart Robotics Factory 4.0 — Digital Twin & Industrial IoT Monitoring

![Unity 6](https://img.shields.io/badge/Unity%206-HDRP-blue.svg?style=for-the-badge&logo=unity)
![Industry 4.0](https://img.shields.io/badge/Industry%204.0-Digital%20Twin-00E5FF.svg?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-Modern%20Async-green.svg?style=for-the-badge&logo=csharp)
![Architecture](https://img.shields.io/badge/Architecture-Event--Driven%20%2F%20Modular-purple.svg?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Desktop%20%2F%20Industrial%20Kiosk-orange.svg?style=for-the-badge)

A comprehensive, production-ready **Industry 4.0 Smart Robotics Digital Twin** developed in **Unity 6 LTS** using the **High Definition Render Pipeline (HDRP)**. Designed for modern smart manufacturing showcases, CV/portfolio demonstrations, and industrial IoT monitoring systems, this project simulates and visualizes a live robotic assembly and automated conveyor factory with deterministic S-curve kinematics, thermodynamic motor modeling, real-time vibration/torque FFT charts, interactive fault injection, and dark-mode glassmorphic telemetry dashboards.

---

## 📑 Table of Contents

- [Core Technical Capabilities](#-core-technical-capabilities)
- [System Architecture](#-system-architecture)
- [Workcell Telemetry & Sensor Schema](#-workcell-telemetry--sensor-schema)
- [Interactive Fault Injection & "What-If" Testing](#-interactive-fault-injection--what-if-testing)
- [Mathematical Kinematics & Physics Models](#-mathematical-kinematics--physics-models)
- [Rendering & Graphical Systems](#-rendering--graphical-systems)
- [Codebase Structure](#-codebase-structure)
- [Technology Stack & Specifications](#-technology-stack--specifications)

---

## 🌟 Core Technical Capabilities

### 1. High-Fidelity 3D Physical Twin
* **HDRP Physical Materials & Lighting**: High-Definition Render Pipeline with custom PBR metallic-smoothness shading, baked global illumination reflection probes, and volumetric lighting.
* **Autonomous AGV & Spline Motion**: Pallets and Automated Guided Vehicles (AGVs) navigate multi-point factory routes using Unity's mathematical Splines package with custom overrun-compensated coroutines.

### 2. Deterministic S-Curve Kinematics & Physics Engine
* **5-Stage Robot Motion Profiles**: Simulates realistic robotic motion (*Pick/Dwell $\rightarrow$ S-Curve Acceleration $\rightarrow$ Cruise Slew $\rightarrow$ Controlled Deceleration $\rightarrow$ Place & Fasten*) with dynamic inertial torque spikes ($68 \sim 74\text{ Nm}$) and steady kinetic friction torques ($34 \sim 38\text{ Nm}$).
* **1st-Order Thermodynamic Motor Model**: Tracks electrical Joule heating ($I^2R$) and ambient convective heat dissipation ($44^\circ\text{C} \sim 48^\circ\text{C}$ steady state).

### 3. Procedural Multi-Axis Waveform FFT Chart
* **Custom Low-Overhead UGUI Mesh Generator**: Inherits from `MaskableGraphic` to dynamically construct vertex buffers every frame ($20\text{ Hz}$ tick), plotting 3-channel dynamic joint torques ($J_1, J_2, J_3$ in $\text{Nm}$) with dashed overload alert threshold markers.

### 4. Dynamic Machine-Specific Fault Injection
* **Context-Aware Testing**: Diagnostic buttons adapt in real time to the selected machine, allowing injection of servo thermal overheating, mechanical torque overloads, gripper vacuum loss, arc current surges, conveyor pallet jams, and grid voltage sags.
* **Vivid Toggle Feedback**: Active fault buttons dynamically highlight in amber (`#E68A00`) or crimson (`#D42A2A`) with `[ACTIVE]` state tagging.

### 5. Bidirectional Physical Safety Interlocks (E-Stop)
* **Plant-Wide Emergency Stop**: Triggering an emergency stop command physically freezes all 3D spline carts, legacy animations, and animators across the entire factory while updating telemetry states and stamping timestamped safety events.

### 6. Cinematic 3/4 Isometric Camera System
* **Anti-Clipping Geometry Protection**: Orbiting inspection camera with smooth damping and height clamping ($0.8\text{m} \le y \le 3.8\text{m}$), providing unobstructed 3/4 isometric views into each robotic cell without clipping into ceiling ducts, walls, or barriers.

---

## 📊 System Architecture

```
                                  [ Physical 3D Factory Floor ]
                                                │
                                    (Bidirectional Sync)
                                                ▼
     ┌────────────────────────────────────────────────────────────────────────┐
     │                     TelemetrySimulatorEngine.cs                        │
     │  - 5-Stage S-Curve Motion Profile       - Joule Heating & Cooling Law │
     │  - Multi-Axis Dynamic Torque (J1,J2,J3) - Real-Time OEE Calculations   │
     │  - Machine-Specific Fault Injection     - Physical Safety E-Stop Loop │
     └────────────────────────────────────────────────────────────────────────┘
                                   │                           │
                   ┌───────────────┘                           └───────────────┐
                   ▼                                                           ▼
┌──────────────────────────────────────┐                   ┌──────────────────────────────────────┐
│       DigitalTwinDashboardHUD        │                   │         LiveWaveformChart            │
│  - Real-Time Kinematics & Gauges     │                   │  - Procedural UGUI Mesh Generator    │
│  - Dynamic Machine Fault Buttons     │                   │  - 3-Axis Dynamic Torque Channels    │
│  - Scrolling Event / Alarm Console   │                   │  - Dynamic Limit Overload Threshold  │
└──────────────────────────────────────┘                   └──────────────────────────────────────┘
                   │                                                           │
                   ▼                                                           ▼
┌──────────────────────────────────────┐                   ┌──────────────────────────────────────┐
│       DigitalTwinCameraController    │                   │          StationVisualizer           │
│  - Smooth Orbit, Pan & Zoom Control  │                   │  - HDRP Emissive Andon Light Towers  │
│  - 5 Calibrated 3/4 Isometric Views  │                   │  - Thermal Stress Material Shaders   │
│  - Height & Boundary Safety Clamps   │                   │  - Click-To-Inspect Raycasting       │
└──────────────────────────────────────┘                   └──────────────────────────────────────┘
```

---

## 🏭 Workcell Telemetry & Sensor Schema

| Workcell ID | Subsystem Name | Monitored Kinematics & Process Metrics | Diagnostics & Health |
| :--- | :--- | :--- | :--- |
| **ST-01** | **6-Axis Robot Assembly Cell 1** | Joint Velocity ($175^\circ/\text{s}$), Motor Torque ($42.0\text{ Nm}$), End-Effector Payload ($14.5\text{ kg}$), Robotic Cycle Time ($7.0\text{s}$), OEE ($94.6\%$) | J3 Servo Temp ($44.0^\circ\text{C}$), Motor Current ($10.8\text{ A}$), Gripper Wear ($16.0\%$), Trajectory Deviation ($0.018\text{ mm}$) |
| **ST-02** | **Robotic Welding & Fastening** | Angular Velocity ($130^\circ/\text{s}$), Weld Torque ($58.0\text{ Nm}$), Arc Current ($14.2\text{ A}$), Robotic Cycle Time ($8.5\text{s}$), OEE ($92.9\%$) | Torch Tip Temp ($49.5^\circ\text{C}$), Current Surge Index, Weld Tip Spatter Sensor, Precision ($0.012\text{ mm}$) |
| **ST-03** | **Conveyor & Vision Sorting Line** | Belt Velocity ($1.25\text{ m/s}$), Drive Torque ($24.5\text{ Nm}$), Part Throughput (PPM), Availability ($99.2\%$), OEE ($99.2\%$) | Optical Vision Pass Rate ($99.6\%$), Infeed Jam Sensor, Belt Slip Coefficient |
| **ST-04** | **Central PLC & Safety Substation** | Power Consumption ($88.5\text{ kW}$), Operating Voltage, Global Plant OEE ($95.2\%$), Fleet Availability ($99.9\%$) | Safety Light Curtain Loops (Active / Tripped), Fieldbus Packet Latency ($2\text{ ms}$) |

---

## 🧪 Interactive Fault Injection & "What-If" Testing

The Digital Twin features real-time anomaly simulation with physical and graphical feedback:

| Target Station | Injected Fault Scenario | Physics & Kinematic Response | UI & 3D Visual Feedback |
| :--- | :--- | :--- | :--- |
| **Robot Cell 1** | **Servo Joint Overheat** | Temperature accelerates to $82.5^\circ\text{C}$ via thermal surge model. | Gauge turns red; Andon light flashes amber; alarm console stamps event. |
| **Robot Cell 1** | **Torque Overload (>180 Nm)** | Mechanical torque spikes to $185\text{ Nm}$; robot joint stalls. | Live waveform line surges past red threshold; machine state flips to `CRITICAL`. |
| **Robot Cell 1** | **Gripper Vacuum Loss** | End-effector payload mass immediately drops to $0.2\text{ kg}$. | Telemetry payload readout shows drop; tooling alarm logged. |
| **Robot Cell 2** | **Torch Thermal Overload** | Torch tip temperature climbs into warning threshold. | Machine badge flips to `WARNING`; thermal shader highlights arm. |
| **Robot Cell 2** | **Arc Current Surge** | Electrical current surges to $28.5\text{ A}$. | Current gauge spikes; power diagnostics alert raised. |
| **Robot Cell 2** | **Weld Tip Jam** | Welding torch locks up against fixture table. | Waveform torque spikes; machine halts cycle. |
| **Conveyor Line** | **Infeed Pallet Jam** | Conveyor velocity drops to $0.02\text{ m/s}$; drive motor torque spikes. | Pallet stops moving; drive motor stall warning issued. |
| **Conveyor Line** | **Belt Slippage** | Belt velocity drops to $0.45\text{ m/s}$ with speed fluctuations. | Conveyor gauge shows speed loss; efficiency degrades. |
| **Conveyor Line** | **Optical Vision Defect** | Optical inspection pass rate drops from $99.6\% \rightarrow 68.5\%$. | Defect counter increments; vision alert triggered. |
| **Central PLC** | **Grid Voltage Surge** | Plant power draw spikes by $+45.0\text{ kW}$. | Substation power readout reflects demand spike. |
| **Central PLC** | **Ethernet Packet Loss** | Fieldbus network latency increases. | Communication warning logged in event console. |
| **Global Plant** | **EMERGENCY STOP** | All kinematic velocities, spline animations, and animators freeze. | `EMERGENCY STOP` button turns flashing red; all machines enter `STOPPED` state. |

---

## 📐 Mathematical Kinematics & Physics Models

### 1. S-Curve Trajectory Profile
Robot arm rotational velocity $v(t)$ is governed by a 5-stage deterministic polynomial profile over cycle duration $T$:
$$v(t) = v_{max} \cdot \left(3\left(\frac{t}{T_{acc}}\right)^2 - 2\left(\frac{t}{T_{acc}}\right)^3\right)$$
Inertial acceleration torque is calculated as:
$$\tau_{acc}(t) = J \cdot \alpha(t) + \tau_{friction}$$

### 2. 1st-Order Thermodynamic Model (Newton's Cooling Law)
Motor servo temperature $T_{motor}(t)$ accounts for Joule heating and ambient convective cooling:
$$\frac{dT_{motor}}{dt} = k_{joule} \cdot \tau_{motor} - h_{conv} \cdot (T_{motor} - T_{ambient})$$
Where $T_{ambient} = 24.0^\circ\text{C}$ and nominal operating temperature stabilizes between $44.0^\circ\text{C} \sim 48.5^\circ\text{C}$.

### 3. Overall Equipment Effectiveness (OEE) Standard
Calculated continuously in accordance with ISO 22400 Industry 4.0 standards:
$$\text{OEE} = \text{Availability} \times \text{Performance} \times \text{Quality}$$

---

## 🎨 Rendering & Graphical Systems

* **Dark-Mode Glassmorphic HUD**: High-contrast, semi-transparent panels with cyan accents (`#00E5FF`), emerald metrics (`#33FF66`), and warning amber (`#FFAA00`).
* **Procedural Vertex Buffer Generation**: `LiveWaveformChart.cs` creates dynamic quad strips directly inside Unity's Canvas renderer without UI instancing overhead.
* **Emissive Andon Light Towers**: Real-time HDRP emissive materials with property blocks switching between Green ($3.5\times$), Flashing Amber ($4.0\times$), and Strobe Red ($5.0\times$).

---

## 📂 Codebase Structure

```
Assets/_Scripts/
├── Camera/
│   └── DigitalTwinCameraController.cs   # Orbit, pan, zoom & 3/4 isometric presets
├── Data/
│   ├── MQTTClientBridge.cs              # Modular enterprise IoT ingestion interface
│   ├── StationTelemetryData.cs          # Data Transfer Objects, enums & OEE schema
│   └── TelemetrySimulatorEngine.cs      # S-curve kinematics & thermodynamic simulator
├── Editor/
│   ├── CameraControllerEditor.cs        # Inspector tool with 1-click Scene View capture
│   ├── DigitalTwinSceneSetup.cs         # 1-click automated scene configuration
│   └── PerformanceOptimizer.cs          # 1-click 60+ FPS performance optimization
├── UI/
│   ├── DigitalTwinDashboardHUD.cs       # Glassmorphic HUD controller & event wireup
│   └── LiveWaveformChart.cs             # Procedural UGUI mesh multi-axis FFT chart
└── Visuals/
    ├── PartInspectableRaycaster.cs      # Hover/click diagnostic raycaster
    └── StationVisualizer.cs             # 3D Andon light towers & thermal shader controllers
```

---

## 🛠️ Technology Stack & Specifications

* **Game Engine**: Unity 6 LTS (`6000.3.9f1`)
* **Render Pipeline**: High Definition Render Pipeline (HDRP)
* **Scripting Runtime**: C# (.NET Standard 2.1 / C# 9.0)
* **Input Architecture**: Unity Input System (`com.unity.inputsystem`)
* **Splines & Kinematics**: Unity Splines (`com.unity.splines`) & Mathematics (`com.unity.mathematics`)
* **UI Framework**: TextMeshPro + Custom MaskableGraphic Procedural Mesh
* **Target Standards**: Industry 4.0 / RAMI 4.0 / ISO 22400 OEE
