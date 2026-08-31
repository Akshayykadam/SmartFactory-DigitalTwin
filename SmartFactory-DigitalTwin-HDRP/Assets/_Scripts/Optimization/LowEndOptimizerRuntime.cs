using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SmartFactory.DigitalTwin.Optimization
{
    public class LowEndOptimizerRuntime : MonoBehaviour
    {
        [Header("Frame Rate & Power")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool optimizeOnAwake = true;

        [Header("Dynamic Resolution Scaling (DRS)")]
        [SerializeField] private bool enableDynamicResolution = true;
        [Range(50f, 100f)] [SerializeField] private float dynamicResolutionPercent = 80f;

        [Header("Light & Shadow Culling")]
        [SerializeField] private float maxShadowDistance = 25f;
        [SerializeField] private bool disablePointLightShadows = true;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0; // Prevent stutters on mismatched refresh rates

            if (optimizeOnAwake)
            {
                ApplyLowEndOptimizations();
            }
        }

        public void ApplyLowEndOptimizations()
        {
            // 1. Cap Shadow Distance
            QualitySettings.shadowDistance = maxShadowDistance;

            // 2. Configure Dynamic Resolution on Main Camera
            Camera cam = Camera.main ?? GetComponent<Camera>();
            if (cam != null)
            {
                var hdCam = cam.GetComponent<HDAdditionalCameraData>();
                if (hdCam != null && enableDynamicResolution)
                {
                    hdCam.allowDynamicResolution = true;
                    DynamicResolutionHandler.SetDynamicResolutionType(DynamicResolutionType.Software);
                }
            }

            // 3. Cull heavy shadows on all point lights
            if (disablePointLightShadows)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type != LightType.Directional)
                    {
                        l.shadows = LightShadows.None;
                        var hdLight = l.GetComponent<HDAdditionalLightData>();
                        if (hdLight != null)
                        {
                            hdLight.EnableShadows(false);
                            hdLight.volumetricDimmer = 0.2f;
                        }
                    }
                }
            }

            Debug.Log($"[Low-End Optimizer] Applied 60 FPS profile: ShadowDist={maxShadowDistance}m, DynamicRes={enableDynamicResolution}");
        }
    }
}
