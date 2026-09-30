using System;
using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 40;
        [SerializeField, Min(0f)] private float damageInvulnerability = 0.45f;
        [SerializeField] private Rigidbody2D physicsBody;

        private float invulnerableUntil;
        private int baseMaximumHealth;

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public bool IsInvulnerable { get; private set; }

        public event Action<Health, int> Damaged;
        public event Action<Health> Died;

        private void Awake()
        {
            if (physicsBody == null) physicsBody = GetComponent<Rigidbody2D>();
            baseMaximumHealth = maximumHealth;
            ResetToFull();
        }

        public void SetMaximumHealthBonus(int bonus, bool healIncrease = false)
        {
            int previousMaximum = maximumHealth;
            maximumHealth = baseMaximumHealth + Mathf.Max(0, bonus);
            CurrentHealth = Mathf.Min(maximumHealth,
                CurrentHealth + (healIncrease ? Mathf.Max(0, maximumHealth - previousMaximum) : 0));
        }

        public bool TakeDamage(int amount, Vector2 knockback)
        {
            if (!IsAlive || IsInvulnerable || amount <= 0) return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            invulnerableUntil = Time.time + damageInvulnerability;
            IsInvulnerable = true;
            if (physicsBody != null && knockback != Vector2.zero)
                physicsBody.AddForce(knockback, ForceMode2D.Impulse);

            Damaged?.Invoke(this, amount);
            if (!IsAlive) Died?.Invoke(this);
            return true;
        }

        public void SetExternalInvulnerability(bool value) => IsInvulnerable = value || Time.time < invulnerableUntil;

        public void ResetToFull()
        {
            CurrentHealth = maximumHealth;
            invulnerableUntil = float.NegativeInfinity;
            IsInvulnerable = false;
        }

        public void Heal(int amount)
        {
            if (IsAlive && amount > 0) CurrentHealth = Mathf.Min(maximumHealth, CurrentHealth + amount);
        }

        private void Update() => IsInvulnerable = Time.time < invulnerableUntil;
    }
}
