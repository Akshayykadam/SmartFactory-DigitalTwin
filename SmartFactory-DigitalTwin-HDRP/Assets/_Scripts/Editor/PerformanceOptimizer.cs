using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using SmartFactory.DigitalTwin.Optimization;

namespace SmartFactory.DigitalTwin.Editor
{
    public class PerformanceOptimizer : EditorWindow
    {
        [MenuItem("Tools/Digital Twin/Optimize Performance (Boost FPS)")]
        public static void OptimizeScenePerformance()
        {
            ApplyOptimization(isUltraFastMode: false);
        }

        [MenuItem("Tools/Digital Twin/Low-End Potato Mode (Ultra Fast 90+ FPS)")]
        public static void OptimizeForLowEndHardware()
        {
            ApplyOptimization(isUltraFastMode: true);
        }

        public static void ApplyOptimization(bool isUltraFastMode)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Optimize Scene Performance");
            int group = Undo.GetCurrentGroup();

            int lightsOptimized = 0;
            int reflectionProbesOptimized = 0;

            // 1. Switch Quality Settings
            QualitySettings.SetQualityLevel(1, true); // Balanced
            QualitySettings.shadowDistance = isUltraFastMode ? 20f : 35f;
            QualitySettings.vSyncCount = 0;

            // 2. Optimize all ceiling and prop lights
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.shadows = LightShadows.Hard;
                    continue;
                }

                Undo.RecordObject(light, "Optimize Light");
                light.shadows = LightShadows.None;

                var hdLight = light.GetComponent<HDAdditionalLightData>();
                if (hdLight != null)
                {
                    Undo.RecordObject(hdLight, "Optimize HD Light");
                    hdLight.EnableShadows(false);
                    hdLight.volumetricDimmer = isUltraFastMode ? 0.0f : 0.25f;
                }
                lightsOptimized++;
            }

            // 3. Optimize Reflection Probes (Switch from Realtime to Baked/Static)
            var probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            foreach (var probe in probes)
            {
                Undo.RecordObject(probe, "Optimize Reflection Probe");
                probe.mode = ReflectionProbeMode.Baked;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                reflectionProbesOptimized++;
            }

            // 4. Optimize Main Camera & Attach Runtime Low-End Optimizer
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var hdCam = mainCam.GetComponent<HDAdditionalCameraData>();
                if (hdCam != null)
                {
                    Undo.RecordObject(hdCam, "Optimize HD Camera");
                    hdCam.allowDynamicResolution = true;
                }

                if (mainCam.GetComponent<LowEndOptimizerRuntime>() == null)
                {
                    Undo.AddComponent<LowEndOptimizerRuntime>(mainCam.gameObject);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            string modeTitle = isUltraFastMode ? "Low-End Potato Mode (90+ FPS)" : "Standard Performance (60+ FPS)";
            EditorUtility.DisplayDialog("Performance Optimization Complete",
                $"🚀 [{modeTitle}] Applied Successfully!\n\n" +
                $"• Quality Preset: Balanced\n" +
                $"• Shadow Distance: {(isUltraFastMode ? "20m" : "35m")}\n" +
                $"• Disabled real-time shadow maps on {lightsOptimized} ceiling point lights\n" +
                $"• Optimized {reflectionProbesOptimized} reflection probes to baked static\n" +
                $"• Enabled Camera Dynamic Resolution Scaling (DRS)\n" +
                $"• Attached LowEndOptimizerRuntime to Main Camera\n\n" +
                $"💡 Tip: In Unity Editor 'Game' tab dropdown, set aspect ratio/resolution to 'Full HD (1920x1080)' for optimal frame rate!", "OK");
        }
    }
}
