using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartFactory.DigitalTwin.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class LiveWaveformChart : MaskableGraphic
    {
        [Header("Chart Settings")]
        [SerializeField] private int maxDataPoints = 80;
        [SerializeField] private float lineWidth = 2.5f;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 150f;

        [Header("Joint Torque Colors")]
        [SerializeField] private Color channelJ1Color = new Color(0.2f, 0.85f, 1f, 1f);   // Cyan (J1 Torque)
        [SerializeField] private Color channelJ2Color = new Color(0.2f, 1.0f, 0.4f, 1f);   // Emerald (J2 Torque)
        [SerializeField] private Color channelJ3Color = new Color(1.0f, 0.75f, 0.2f, 1f);  // Amber (J3 Torque)
        [SerializeField] private Color thresholdColor = new Color(1.0f, 0.2f, 0.2f, 0.6f);// Overload Alert

        [Header("Overload Threshold")]
        [SerializeField] private float torqueLimitThreshold = 100f; // 100 Nm warning

        private readonly List<float> dataJ1 = new List<float>();
        private readonly List<float> dataJ2 = new List<float>();
        private readonly List<float> dataJ3 = new List<float>();

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < maxDataPoints; i++)
            {
                dataJ1.Add(38f);
                dataJ2.Add(45f);
                dataJ3.Add(29f);
            }
        }

        public void PushTelemetryValues(Vector3 jointTorques)
        {
            if (dataJ1.Count >= maxDataPoints) dataJ1.RemoveAt(0);
            if (dataJ2.Count >= maxDataPoints) dataJ2.RemoveAt(0);
            if (dataJ3.Count >= maxDataPoints) dataJ3.RemoveAt(0);

            dataJ1.Add(jointTorques.x);
            dataJ2.Add(jointTorques.y);
            dataJ3.Add(jointTorques.z);

            SetVerticesDirty();
        }

        public void ClearData()
        {
            dataJ1.Clear();
            dataJ2.Clear();
            dataJ3.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float width = rect.width;
            float height = rect.height;

            if (dataJ1.Count < 2) return;

            // Draw Torque Threshold Line
            float threshNormY = Mathf.InverseLerp(minValue, maxValue, torqueLimitThreshold);
            float threshY = rect.yMin + threshNormY * height;
            DrawDashedLine(vh, new Vector2(rect.xMin, threshY), new Vector2(rect.xMax, threshY), thresholdColor, 1.5f, 6f, 4f);

            // Draw Channels (J1, J2, J3)
            DrawChannelLine(vh, dataJ1, channelJ1Color, rect, width, height);
            DrawChannelLine(vh, dataJ2, channelJ2Color, rect, width, height);
            DrawChannelLine(vh, dataJ3, channelJ3Color, rect, width, height);
        }

        private void DrawChannelLine(VertexHelper vh, List<float> points, Color color, Rect rect, float width, float height)
        {
            float stepX = width / (maxDataPoints - 1);

            for (int i = 0; i < points.Count - 1; i++)
            {
                float normY1 = Mathf.Clamp01(Mathf.InverseLerp(minValue, maxValue, points[i]));
                float normY2 = Mathf.Clamp01(Mathf.InverseLerp(minValue, maxValue, points[i + 1]));

                Vector2 p1 = new Vector2(rect.xMin + i * stepX, rect.yMin + normY1 * height);
                Vector2 p2 = new Vector2(rect.xMin + (i + 1) * stepX, rect.yMin + normY2 * height);

                DrawThickLineSegment(vh, p1, p2, color, lineWidth);
            }
        }

        private void DrawThickLineSegment(VertexHelper vh, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 dir = (end - start).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

            int startIndex = vh.currentVertCount;

            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddVert(start - normal, color, Vector2.zero);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private void DrawDashedLine(VertexHelper vh, Vector2 start, Vector2 end, Color color, float thickness, float dashLen, float gapLen)
        {
            float totalDist = Vector2.Distance(start, end);
            Vector2 dir = (end - start).normalized;
            float currentDist = 0f;

            while (currentDist < totalDist)
            {
                float len = Mathf.Min(dashLen, totalDist - currentDist);
                Vector2 segStart = start + dir * currentDist;
                Vector2 segEnd = segStart + dir * len;

                DrawThickLineSegment(vh, segStart, segEnd, color, thickness);
                currentDist += dashLen + gapLen;
            }
        }
    }
}
