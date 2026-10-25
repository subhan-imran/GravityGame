using UnityEngine;

namespace GravityGame
{
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class OxygenCape : MonoBehaviour
    {
        public Color fullOxygenBlue = new Color(0.035f, 0.25f, 0.95f);
        public Color emptyOxygenRed = new Color(0.95f, 0.035f, 0.065f);
        public Color DisplayedColor { get; private set; }
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private SphericalPlayer player;
        private Renderer capeRenderer;
        private MaterialPropertyBlock properties;
        private bool hasColor;

        private void OnEnable() => RefreshFromPlayer();
        private void LateUpdate()
        {
            if (player != null) SetOxygenVisual(player.OxygenFraction);
        }

        public void RefreshFromPlayer()
        {
            if (player == null) player = GetComponentInParent<SphericalPlayer>();
            SetOxygenVisual(player != null ? player.OxygenFraction : 1f);
        }

        // Also supports standalone art previews without changing gameplay oxygen.
        public void SetOxygenVisual(float fraction)
        {
            Color color = Color.Lerp(emptyOxygenRed, fullOxygenBlue, Mathf.Clamp01(fraction));
            if (hasColor && DisplayedColor == color) return;
            if (capeRenderer == null) capeRenderer = GetComponent<Renderer>();
            if (properties == null) properties = new MaterialPropertyBlock();
            capeRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, color);
            properties.SetColor(EmissionColor, color * 0.12f);
            capeRenderer.SetPropertyBlock(properties);
            DisplayedColor = color;
            hasColor = true;
        }
    }
}
