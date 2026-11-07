using UnityEngine;
using UnityEngine.EventSystems;

namespace LaserHero
{
    [DefaultExecutionOrder(-50)]
    public class LaserHeroPlayer : MonoBehaviour
    {
        public Camera view;
        public Animator animator;
        public Transform head;
        public float speed = 7, yaw, pitch = 6;
        public float flightCeiling = 65;
        public bool MenuOpen;
        public LaserFlightMotor flightMotor;
        public bool Flying => flightMotor != null && flightMotor.Flying;

        public bool ToggleFlight()
        {
            if (flightMotor == null)
                flightMotor = GetComponent<LaserFlightMotor>();
            return flightMotor != null && flightMotor.ToggleFlight();
        }

        public Vector2 touchMove, touchLook;
        public bool touchJump;
        CharacterController controller;
        float vertical;
        float shake;
        float shakeStarted;
        public float ShakeAmplitude => shake * Mathf.Clamp01(1 - (Time.unscaledTime - shakeStarted) / .3f);

        public void AddShake(float amount)
        {
            if (amount <= 0)
                return;
            shake = Mathf.Min(.65f, Mathf.Max(ShakeAmplitude, amount));
            shakeStarted = Time.unscaledTime;
        }

        public void ClearShake()
        {
            shake = 0;
        }

        Vector3 spawn;
        public Vector3 EyeCenter => head != null ? head.position + Vector3.up * .18f + transform.forward * .22f
            : transform.position + Vector3.up * .85f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
        }

        void Update()
        {
            if (!controller.enabled)
                return;
            if (Flying)
                return;
            if (MenuOpen)
                touchLook = Vector2.zero;
            if (!MenuOpen)
            {
                bool orbit = Input.GetMouseButton(1);
                if (orbit)
                {
                    yaw += Input.GetAxis("Mouse X") * 3;
                    pitch -= Input.GetAxis("Mouse Y") * 2;
                }

                yaw += touchLook.x * .13f;
                pitch -= touchLook.y * .13f;
                touchLook = Vector2.zero;
                pitch = Mathf.Clamp(pitch, -30, 65);
                Vector2 move = touchMove + new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                move = Vector2.ClampMagnitude(move, 1);
                Vector3 motion = Quaternion.Euler(0, yaw, 0) * new Vector3(move.x, 0, move.y);
                if (motion.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(motion), Time.deltaTime * 14);
                {
                    if (controller.isGrounded && vertical < 0)
                        vertical = -2;
                    if ((Input.GetKeyDown(KeyCode.Space) || touchJump) && controller.isGrounded)
                        vertical = 9;
                    touchJump = false;
                    vertical -= 24 * Time.deltaTime;
                    controller.Move((motion * speed + Vector3.up * vertical) * Time.deltaTime);
                }

                if (animator != null)
                {
                    animator.SetFloat("Magnitude", move.magnitude);
                    animator.SetBool("isRunning", move.magnitude > .1f);
                    animator.SetBool("isGrounded", controller.isGrounded);
                    animator.SetFloat("Velocity", vertical);
                }
            }

            if (transform.position.y < -12)
                Teleport(spawn);
        }

        void LateUpdate()
        {
            if (view == null)
                return;
            // Cinemachine owns the camera pose; its extension applies our impact shake.
            if (view.TryGetComponent<Unity.Cinemachine.CinemachineBrain>(out var brain) && brain.isActiveAndEnabled)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 center = transform.position + Vector3.up * .75f;
            Vector3 offset = rotation * new Vector3(0, 0, -7);
            float length = offset.magnitude;
            if (Physics.SphereCast(center, .2f, offset.normalized, out var hit, length, 1, QueryTriggerInteraction.Ignore))
                length = Mathf.Max(.7f, hit.distance - .15f);
            Vector3 kick = new Vector3(Mathf.PerlinNoise(Time.unscaledTime * 29, 0) - .5f, Mathf.PerlinNoise(0,
                Time.unscaledTime * 33) - .5f, 0) * ShakeAmplitude;
            view.transform.SetPositionAndRotation(center + offset.normalized * length + kick,
                rotation * Quaternion.Euler(kick.y * 2, kick.x * 2, kick.x * 1.5f));
        }

        public void Face(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 12);
        }

        public void Teleport(Vector3 point)
        {
            if (flightMotor != null)
                flightMotor.StopFlight();
            controller.enabled = false;
            transform.position = point;
            vertical = 0;
            controller.enabled = true;
        }
    }

    public class LaserTouchPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public LaserHeroPlayer player;
        public bool movement;
        public RectTransform knob;
        Vector2 start;
        int activePointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        {
            if (activePointer != int.MinValue)
                return;
            activePointer = e.pointerId;
            start = e.position;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != activePointer)
                return;
            if (movement)
            {
                Vector2 d = Vector2.ClampMagnitude((e.position - start) / 65, 1);
                player.touchMove = d;
                if (knob != null)
                    knob.anchoredPosition = d * 35;
            }
            else
                player.touchLook += e.delta;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != activePointer)
                return;
            activePointer = int.MinValue;
            if (movement)
            {
                player.touchMove = Vector2.zero;
                if (knob != null)
                    knob.anchoredPosition = Vector2.zero;
            }
        }

        void OnDisable()
        {
            activePointer = int.MinValue;
            if (player != null)
                player.touchMove = Vector2.zero;
        }
    }
}
