using UnityEngine;

namespace LaserHero
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaserArchBlock : MonoBehaviour
    {
        [ColorUsage(false)]
        public Color blockColor = new Color(.36f, .26f, 1);
        Renderer surface;
        MaterialPropertyBlock properties;
        Color appliedColor;
        Vector3 appliedScale;
        void OnEnable()
        {
            ApplyColor();
        }

        void OnValidate()
        {
            ApplyColor();
        }

        void Update()
        {
            if (appliedColor != blockColor || appliedScale != transform.lossyScale)
                ApplyColor();
        }

        public void ApplyColor()
        {
            if (surface == null)
                surface = GetComponent<Renderer>();
            if (surface == null)
                return;
            if (properties == null)
                properties = new MaterialPropertyBlock();
            surface.GetPropertyBlock(properties);
            properties.SetColor("_Color", blockColor);
            properties.SetFloat("_UseVertexColor", 0);
            properties.SetFloat("_IsDynamic", 1);
            Vector3 scale = transform.lossyScale;
            properties.SetVector("_ObjectScale", new Vector4(scale.x, scale.y, scale.z, 1));
            surface.SetPropertyBlock(properties);
            appliedColor = blockColor;
            appliedScale = scale;
        }
    }
}
