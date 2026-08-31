using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SmartFactory.DigitalTwin.Editor
{
    public class PerformanceOptimizer : EditorWindow
    {
        [MenuItem("Tools/Digital Twin/Optimize Performance (Boost FPS)")]
        public static void OptimizeScenePerformance()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Optimize Scene Performance");
            int group = Undo.GetCurrentGroup();

            int lightsOptimized = 0;
            int reflectionProbesOptimized = 0;

            // 1. Switch Quality Settings to Balanced or Performant
            QualitySettings.SetQualityLevel(1, true); // 1 = Balanced
            Debug.Log("[Performance Optimizer] Switched Project Quality Level to 'Balanced'.");

            // 2. Optimize all ceiling and prop lights (disable real-time shadows on decorative lights)
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                // Keep directional sun/main key lights with shadows, optimize all point/spot/ceiling lights
                if (light.type == LightType.Directional) continue;

                Undo.RecordObject(light, "Optimize Light");
                light.shadows = LightShadows.None;

                var hdLight = light.GetComponent<HDAdditionalLightData>();
                if (hdLight != null)
                {
                    Undo.RecordObject(hdLight, "Optimize HD Light");
                    // Disable shadow map on secondary lights
                    hdLight.EnableShadows(false);
                    hdLight.volumetricDimmer = 0.5f; // reduce volumetric cost
                }
                lightsOptimized++;
            }

            // 3. Optimize Reflection Probes
            var probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            foreach (var probe in probes)
            {
                Undo.RecordObject(probe, "Optimize Reflection Probe");
                probe.mode = ReflectionProbeMode.Baked;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                reflectionProbesOptimized++;
            }

            // 4. Optimize Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var hdCam = mainCam.GetComponent<HDAdditionalCameraData>();
                if (hdCam != null)
                {
                    Undo.RecordObject(hdCam, "Optimize HD Camera");
                    hdCam.allowDynamicResolution = true;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayDialog("Performance Optimization Complete",
                $"🚀 Performance has been optimized for high FPS!\n\n" +
                $"• Quality Preset set to 'Balanced'\n" +
                $"• Disabled real-time shadows on {lightsOptimized} decorative ceiling lights\n" +
                $"• Optimized {reflectionProbesOptimized} reflection probes\n" +
                $"• Enabled Camera Dynamic Resolution\n\n" +
                $"💡 Tip for Editor: In the 'Game' tab dropdown, set resolution to 'Full HD (1920x1080)' for the smoothest 60+ FPS playback!", "OK");
        }
    }
}
