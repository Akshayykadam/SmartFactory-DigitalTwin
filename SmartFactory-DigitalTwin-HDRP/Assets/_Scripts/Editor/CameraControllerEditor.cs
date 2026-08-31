using UnityEngine;
using UnityEditor;
using SmartFactory.DigitalTwin.Navigation;

namespace SmartFactory.DigitalTwin.Editor
{
    [CustomEditor(typeof(DigitalTwinCameraController))]
    public class CameraControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (DigitalTwinCameraController)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎥 Camera Angle Tuning & Capture Tool", EditorStyles.boldLabel);

            if (GUILayout.Button("📷 Capture Scene View to Active Preset", GUILayout.Height(32)))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    Camera sceneCam = SceneView.lastActiveSceneView.camera;
                    Vector3 pivot = SceneView.lastActiveSceneView.pivot;
                    float dist = SceneView.lastActiveSceneView.size;
                    Vector3 euler = sceneCam.transform.rotation.eulerAngles;

                    controller.SetPresetValues(controller.CurrentPresetIndex, pivot, dist, euler.y, euler.x);
                    EditorUtility.SetDirty(controller);

                    Debug.Log($"[Camera Tuner] Captured Scene View into Preset {controller.CurrentPresetIndex}: Pivot={pivot}, Dist={dist:F1}, Yaw={euler.y:F1}, Pitch={euler.x:F1}");
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Test Presets (Editor & Play Mode):", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0: Overview")) controller.FocusPreset(0);
            if (GUILayout.Button("1: Robot 1")) controller.FocusPreset(1);
            if (GUILayout.Button("2: Robot 2")) controller.FocusPreset(2);
            if (GUILayout.Button("3: Conveyor")) controller.FocusPreset(3);
            if (GUILayout.Button("4: PLC")) controller.FocusPreset(4);
            EditorGUILayout.EndHorizontal();
        }
    }
}
