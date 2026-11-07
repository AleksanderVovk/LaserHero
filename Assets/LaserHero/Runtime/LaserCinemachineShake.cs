using Unity.Cinemachine;
using UnityEngine;

namespace LaserHero
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Laser Hero/Cinemachine Impact Shake")]
    public sealed class LaserCinemachineShake : CinemachineExtension
    {
        public LaserHeroPlayer hero;
        [Min(0)]
        public float positionGain = .55f, rotationGain = 3.2f;
        [Min(1)]
        public float frequency = 28;
        public Vector3 LastPositionOffset { get; private set; }
        public Vector3 LastRotationOffset { get; private set; }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Noise)
                return;
            LastPositionOffset = LastRotationOffset = Vector3.zero;
            if (!Application.isPlaying || hero == null || hero.MenuOpen || deltaTime < 0)
                return;
            float strength = hero.ShakeAmplitude;
            if (strength <= 0)
                return;
            float t = Time.unscaledTime * frequency;
            float x = (Mathf.PerlinNoise(t, 13.7f) - .5f) * 2;
            float y = (Mathf.PerlinNoise(37.1f, t) - .5f) * 2;
            float z = (Mathf.PerlinNoise(t, 71.9f) - .5f) * 2;
            var local = new Vector3(x, y, z * .25f) * strength * positionGain;
            LastPositionOffset = state.RawOrientation * local;
            LastRotationOffset = new Vector3(y, -x, z * .35f) * strength * rotationGain;
            state.PositionCorrection += LastPositionOffset;
            state.OrientationCorrection *= Quaternion.Euler(LastRotationOffset);
        }
    }
}
