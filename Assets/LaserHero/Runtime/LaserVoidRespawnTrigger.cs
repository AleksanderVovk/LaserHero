using UnityEngine;

namespace LaserHero
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LaserVoidRespawnTrigger : MonoBehaviour
    {
        public Player player;
        [Tooltip("Also catches falls that skip the trigger or happen beyond its edges.")]
        public bool catchBelowTrigger = true;
        BoxCollider volume;
        float nextRespawn;
        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void Awake()
        {
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
            if (player == null)
                player = FindFirstObjectByType<Player>();
        }

        void OnTriggerEnter(Collider other)
        {
            ReturnPlayer(other.GetComponentInParent<Player>());
        }

        void OnTriggerStay(Collider other)
        {
            ReturnPlayer(other.GetComponentInParent<Player>());
        }

        void FixedUpdate()
        {
            if (catchBelowTrigger && player != null && player.transform.position.y < volume.bounds.max.y)
                ReturnPlayer(player);
        }

        void ReturnPlayer(Player target)
        {
            if (target == null || target != player || target.spawnPoint == null || Time.unscaledTime < nextRespawn)
                return;
            nextRespawn = Time.unscaledTime + .5f;
            var flight = target.GetComponent<LaserFlightMotor>();
            if (flight != null)
                flight.StopFlight();
            target.TeleportToSpawn();
            var controller = target.GetComponent<PlayerController>();
            if (controller != null)
                controller.ResetForBuildLevel();
        }
    }
}
