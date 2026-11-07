using UnityEngine;

namespace LaserHero
{
    public class LaserDroneVisual : MonoBehaviour
    {
        public Transform[] rotors;
        public Transform muzzle;
        [Min(0)]
        public float previewRotationSpeed = 22;
        [Min(0)]
        public float previewBobHeight = .2f;
        [Min(0)]
        public float previewBobFrequency = .4f;
        bool preview;
        Vector3 previewPosition;
        Quaternion previewRotation;
        float previewTime;
        void Awake()
        {
            preview = GetComponentInParent<LaserDroneShopTrigger>() != null;
            previewPosition = transform.localPosition;
            previewRotation = transform.localRotation;
        }

        void Update()
        {
            if (preview)
            {
                previewTime += Time.deltaTime;
                transform.localPosition = previewPosition + Vector3.up * (Mathf.Sin(previewTime * Mathf.PI
                    * 2 * previewBobFrequency) * previewBobHeight);
                transform.localRotation = previewRotation * Quaternion.Euler(0, previewTime * previewRotationSpeed, 0);
            }

            if (rotors == null)
                return;
            for (int i = 0; i < rotors.Length; i++)
                if (rotors[i] != null)
                    rotors[i].Rotate(0, (i % 2 == 0 ? 1 : -1) * 1800 * Time.deltaTime, 0, Space.Self);
        }
    }
}
