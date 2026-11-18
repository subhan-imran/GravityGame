using UnityEngine;

namespace GravityGame
{
    [ExecuteAlways, RequireComponent(typeof(PlanetGravity))]
    public sealed class GravityBoundary : MonoBehaviour
    {
        [Tooltip("Optional rings. Update automatically when radius or gravity range changes.")]
        public LineRenderer[] rings = new LineRenderer[0];
        [Min(12)] public int segments = 128;
        [Min(0f)] public float width = 0.035f;
        private float previousRadius = -1f, previousRange, previousScale;
        private int previousSegments;
        private float previousWidth;
        private void OnValidate() => Refresh();
        private void Update()
        {
            var source = GetComponent<PlanetGravity>();
            if (source.Radius != previousRadius || source.influenceHeight != previousRange ||
                transform.lossyScale.x != previousScale || segments != previousSegments || width != previousWidth)
                Refresh();
        }
        public void Refresh()
        {
            var source = GetComponent<PlanetGravity>();
            float scale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            previousRadius = source.Radius; previousRange = source.influenceHeight;
            previousScale = transform.lossyScale.x; previousSegments = segments; previousWidth = width;
            int count = Mathf.Max(12, segments);
            float radius = (source.Radius + source.influenceHeight) / scale;
            for (int plane = 0; plane < rings.Length; plane++)
            {
                var line = rings[plane];
                if (line == null) continue;
                line.useWorldSpace = false; line.loop = true;
                line.positionCount = count; line.widthMultiplier = width / scale;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2f / count;
                    float x = Mathf.Cos(angle) * radius, y = Mathf.Sin(angle) * radius;
                    line.SetPosition(i, plane % 3 == 0 ? new Vector3(x, y, 0f) :
                        plane % 3 == 1 ? new Vector3(x, 0f, y) : new Vector3(0f, x, y));
                }
            }
        }
    }
}
