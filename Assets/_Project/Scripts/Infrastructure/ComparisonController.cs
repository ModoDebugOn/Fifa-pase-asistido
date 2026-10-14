using System;
using System.Collections.Generic;
using System.Linq;
using AssistedPass.Application;
using AssistedPass.Domain;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Side-by-side comparison: reads the input ONCE and applies the same aim and the same pass
    /// to every lane (one Play per assist level). When every pass is over, it resets all lanes
    /// together, so they always start from the same situation.
    /// </summary>
    public class ComparisonController : MonoBehaviour
    {
        /// <summary>One side of the screen: its Play, its use cases and its camera.</summary>
        public sealed class Lane
        {
            public Play Play { get; }
            public EvaluatePass Evaluate { get; }
            public ExecutePass Pass { get; }
            public ResetPlay Reset { get; }
            public Func<Player, PlayerView> FindView { get; }
            public Camera Camera { get; }

            public Lane(Play play, EvaluatePass evaluate, ExecutePass pass, ResetPlay reset,
                        Func<Player, PlayerView> findView, Camera camera)
            {
                Play = play; Evaluate = evaluate; Pass = pass; Reset = reset;
                FindView = findView; Camera = camera;
            }

            public PlayerView HolderView => FindView(Play.Holder);
        }

        [Tooltip("Below this value the gamepad stick is considered at rest.")]
        [SerializeField, Range(0f, 0.9f)] float deadZone = 0.25f;

        [Tooltip("Seconds after every pass is over before both sides start again.")]
        [SerializeField, Min(0f)] float resetDelay = 1.5f;

        IReadOnlyList<Lane> lanes;
        PassInput input;
        bool passMade;
        float timeSinceResolved;

        public void Initialize(IReadOnlyList<Lane> lanes)
        {
            this.lanes = lanes;
            input = new PassInput(lanes[0].Camera, deadZone);
        }

        public void ResetAll()
        {
            foreach (Lane lane in lanes) lane.Reset.Execute();
            passMade = false;
            timeSinceResolved = 0f;
        }

        void Update()
        {
            if (lanes == null) return;

            if (input.ResetPressed)
            {
                ResetAll();
                return;
            }

            if (!passMade) Aim();
            else ResetWhenResolved();
        }

        void Aim()
        {
            // The mouse aims on the half of the screen it is over; the result is the same
            // direction for every lane, because both pitches are identical.
            Lane reference = LaneUnderMouse();
            input.Camera = reference.Camera;
            Vector3 direction = input.ReadDirection(reference.HolderView.transform.position);
            var pitchDirection = PitchConversion.ToPitch(direction);

            foreach (Lane lane in lanes)
            {
                lane.HolderView.LookTowards(direction);
                lane.Evaluate.Execute(pitchDirection);
            }

            if (!input.PassPressed) return;
            foreach (Lane lane in lanes) lane.Pass.Execute(pitchDirection);
            passMade = true;
        }

        void ResetWhenResolved()
        {
            if (lanes.Any(l => l.Play.Phase == PlayPhase.BallInFlight)) return;

            timeSinceResolved += Time.deltaTime;
            if (timeSinceResolved >= resetDelay) ResetAll();
        }

        Lane LaneUnderMouse()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return lanes[0];
            Vector2 position = mouse.position.ReadValue();
            return lanes.FirstOrDefault(l => l.Camera.pixelRect.Contains(position)) ?? lanes[0];
        }
    }
}
