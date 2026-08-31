using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartFactory.DigitalTwin.Visuals
{
    public class PartInspectableRaycaster : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Part Metadata")]
        [SerializeField] private string partNumber = "P-10482";
        [SerializeField] private string partName = "High-Torque Spindle Servo Motor";
        [SerializeField] private string manufacturer = "Siemens / Fanuc";
        [SerializeField] private float operatingHours = 4120f;
        [SerializeField] private float healthScore = 94.2f;

        [Header("Hover Visuals")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.8f, 1.0f, 0.4f);

        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public static event Action<PartInspectableRaycaster> OnPartSelected;
        public static event Action OnPartDeselected;

        public string PartNumber => partNumber;
        public string PartName => partName;
        public string Manufacturer => manufacturer;
        public float OperatingHours => operatingHours;
        public float HealthScore => healthScore;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();

            // Ensure collider exists for raycasting
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HighlightMesh(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HighlightMesh(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnPartSelected?.Invoke(this);
        }

        private void HighlightMesh(bool active)
        {
            if (targetRenderer == null) return;

            targetRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorId, active ? highlightColor * 2.5f : Color.black);
            targetRenderer.SetPropertyBlock(propBlock);
        }
    }
}
