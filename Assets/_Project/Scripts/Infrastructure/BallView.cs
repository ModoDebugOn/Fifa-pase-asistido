using System;
using AssistedPass.Application;
using AssistedPass.Domain;
using UnityEngine;
using PitchVector = System.Numerics.Vector2;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Implements the IBall port with the sphere in the scene: it sticks it to the holder's feet
    /// or rolls it in a straight line, at constant speed, to the target (a ground pass).
    /// </summary>
    public class BallView : MonoBehaviour, IBall
    {
        [Tooltip("Pass speed (m/s).")]
        [SerializeField, Min(1f)] float passSpeed = 22f;

        [Tooltip("Distance of the ball in front of the player who has it (m).")]
        [SerializeField] float footOffset = 0.6f;

        Func<Player, PlayerView> findView;
        PlayerView holder;
        bool inFlight;
        Vector3 target;
        Action onArrival;

        float Radius => transform.localScale.y * 0.5f;

        public void Initialize(Func<Player, PlayerView> findView) => this.findView = findView;

        public void GiveTo(Player player)
        {
            holder = findView(player);
            inFlight = false;
            onArrival = null;
            StickToFoot();
        }

        public void Pass(PitchVector target, Action onArrival)
        {
            holder = null;
            this.target = PitchConversion.ToWorld(target, Radius);
            this.onArrival = onArrival;
            inFlight = true;
        }

        void Update()
        {
            if (holder != null) { StickToFoot(); return; }
            if (!inFlight) return;

            Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(before, target, passSpeed * Time.deltaTime);
            Roll(transform.position - before);

            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                inFlight = false;
                Action callback = onArrival;
                onArrival = null;
                callback?.Invoke(); // may call GiveTo: that is why it is cleared first
            }
        }

        void StickToFoot()
        {
            Vector3 feet = holder.transform.position;
            feet.y = 0f;
            transform.position = feet + holder.transform.forward * footOffset + Vector3.up * Radius;
        }

        // Rotates the ball according to how far it moved, so it visibly rolls.
        void Roll(Vector3 displacement)
        {
            float distance = displacement.magnitude;
            if (distance < 0.0001f) return;
            Vector3 axis = Vector3.Cross(Vector3.up, displacement / distance);
            transform.Rotate(axis, distance / Radius * Mathf.Rad2Deg, Space.World);
        }
    }
}
