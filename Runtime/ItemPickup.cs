using System;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>ItemPickup</b> is the MonoBehaviour placed on world-item prefabs.
    /// It handles collision/trigger detection for auto-collect, exposes an
    /// <see cref="Interact"/> method for interact-style pickups, and delegates
    /// the actual collection logic back to <see cref="ItemManager"/> via event.
    /// </summary>
    [AddComponentMenu("ItemManager/Item Pickup")]
    [DisallowMultipleComponent]
    public class ItemPickup : MonoBehaviour
    {
        // ─── Data ────────────────────────────────────────────────────────────────

        /// <summary>The definition that created this pickup instance.</summary>
        [HideInInspector] public ItemWorldDefinition Definition;

        /// <summary>Unique instance id assigned by ItemManager.</summary>
        [HideInInspector] public string InstanceId;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the pickup is collected (either via trigger, interact, or API).
        /// Parameters: definitionId, instanceId, gameObject.
        /// Subscribed by <see cref="ItemManager"/>.
        /// </summary>
        public event Action<string, string, GameObject> OnCollected;

        // ─── State ───────────────────────────────────────────────────────────────

        private bool _collected;

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Call to collect this item from code or input (Interact behaviour).
        /// Safe to call multiple times — only fires once per instance.
        /// </summary>
        public void Interact()
        {
            if (_collected) return;
            Collect();
        }

        /// <summary>
        /// Immediately collect this pickup without physics or player proximity checks.
        /// Used internally and by the ItemManager's <c>CollectItem</c> API.
        /// </summary>
        public void Collect()
        {
            if (_collected) return;
            _collected = true;
            OnCollected?.Invoke(Definition?.id, InstanceId, gameObject);
        }

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (Definition == null) return;
            if (Definition.pickupBehaviour != ItemPickupBehaviour.AutoCollect) return;

            // Collect when any collider with the "Player" tag enters
            if (other.CompareTag("Player"))
                Collect();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected) return;
            if (Definition == null) return;
            if (Definition.pickupBehaviour != ItemPickupBehaviour.AutoCollect) return;

            if (other.CompareTag("Player"))
                Collect();
        }

        // ─── Reset state (called by ItemManager when re-spawning from pool) ──────

        /// <summary>Reset collected state so the pickup can be reused from a pool.</summary>
        public void ResetPickup()
        {
            _collected = false;
        }
    }
}
