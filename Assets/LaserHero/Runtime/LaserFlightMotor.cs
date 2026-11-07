using UnityEngine;

namespace LaserHero
{
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class LaserFlightMotor : MonoBehaviour
    {
        public LaserHeroPlayer hero;
        public Collider lobbyGround;
        public Bounds fallbackLobby = new Bounds(new Vector3(0, 0, 3), new Vector3(86, 10, 62));
        public float horizontalSpeed = 11.2f, verticalSpeed = 7, takeoffDuration = .28f;
        public bool Flying { get; private set; }

        [HideInInspector]
        public bool ascendHeld, descendHeld;
        public bool InLobby => ContainsLobby(transform.position);

        public bool ContainsLobby(Vector3 point)
        {
            var b = lobbyGround != null ? lobbyGround.bounds : fallbackLobby;
            return point.x >= b.min.x && point.x <= b.max.x && point.z >= b.min.z && point.z <= b.max.z;
        }

        CharacterController controller;
        InputsManager inputs;
        PlayerState state;
        Vector3 velocity;
        float takeoffUntil;
        int flightLayer = -1;
        static readonly int FlyingId = Animator.StringToHash("LaserFlying");
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputs = GetComponent<InputsManager>();
            state = GetComponent<PlayerState>();
            if (hero == null)
                hero = GetComponent<LaserHeroPlayer>();
            hero.flightMotor = this;
            if (hero.animator != null)
                flightLayer = hero.animator.GetLayerIndex("Flight Pose");
        }

        public bool ToggleFlight()
        {
            if (Flying)
            {
                StopFlight();
                return true;
            }

            if (hero == null || hero.MenuOpen || InLobby || state != null && state.isDeath)
                return false;
            Flying = true;
            velocity = Vector3.zero;
            takeoffUntil = Time.time + takeoffDuration;
            if (hero.animator != null)
                hero.animator.SetBool(FlyingId, true);
            return true;
        }

        public void StopFlight()
        {
            Flying = false;
            ascendHeld = descendHeld = false;
            velocity = Vector3.zero;
            if (hero != null && hero.animator != null)
                hero.animator.SetBool(FlyingId, false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
                ToggleFlight();
            if (flightLayer >= 0 && hero.animator != null)
                hero.animator.SetLayerWeight(flightLayer,
                    Mathf.MoveTowards(hero.animator.GetLayerWeight(flightLayer), Flying ? 1 : 0, Time.deltaTime * 5));
            if (!Flying)
                return;
            if (InLobby || state != null && state.isDeath)
            {
                StopFlight();
                return;
            }

            if (hero.MenuOpen || !controller.enabled)
            {
                velocity = Vector3.zero;
                return;
            }

            Vector2 move = inputs != null && inputs.enabled ? inputs.inputDirection
                : hero.touchMove + new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            move = Vector2.ClampMagnitude(move, 1);
            var camera = hero.view != null ? hero.view.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float vertical = (ascendHeld || Input.GetKey(KeyCode.Space) ? 1 : 0) - (descendHeld
                || Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
            if (Time.time < takeoffUntil)
                vertical = Mathf.Max(vertical, 1);
            var target = (forward * move.y + right * move.x) * horizontalSpeed + Vector3.up * vertical * verticalSpeed;
            velocity = Vector3.Lerp(velocity, target, 1 - Mathf.Exp(-14 * Time.deltaTime));
            if (transform.position.y >= hero.flightCeiling)
                velocity.y = Mathf.Min(0, velocity.y);
            controller.Move(velocity * Time.deltaTime);
            if (move.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(forward * move.y + right * move.x), Time.deltaTime * 9);
        }

        void OnDisable()
        {
            StopFlight();
            if (flightLayer >= 0 && hero.animator != null)
                hero.animator.SetLayerWeight(flightLayer, 0);
        }
    }
}
