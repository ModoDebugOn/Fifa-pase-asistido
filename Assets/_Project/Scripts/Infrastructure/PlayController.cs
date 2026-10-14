using System;
using AssistedPass.Application;
using AssistedPass.Domain;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Reads the input every frame and calls the use cases:
    /// aim → EvaluatePass, press pass → ExecutePass, reset → ResetPlay.
    /// After a lost pass, it resets on its own after a moment.
    /// </summary>
    public class PlayController : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;

        [Tooltip("Below this value the gamepad stick is considered at rest.")]
        [SerializeField, Range(0f, 0.9f)] float deadZone = 0.25f;

        [Tooltip("Seconds after a lost pass before the ball goes back to the initial holder.")]
        [SerializeField, Min(0f)] float resetDelayAfterLoss = 1.5f;

        Play play;
        EvaluatePass evaluate;
        ExecutePass pass;
        ResetPlay reset;
        Func<Player, PlayerView> findView;
        PassInput input;
        float timeSinceLoss;

        public void Initialize(Play play, EvaluatePass evaluate, ExecutePass pass,
                               ResetPlay reset, Func<Player, PlayerView> findView)
        {
            this.play = play;
            this.evaluate = evaluate;
            this.pass = pass;
            this.reset = reset;
            this.findView = findView;
            input = new PassInput(targetCamera != null ? targetCamera : Camera.main, deadZone);
        }

        void Update()
        {
            if (play == null) return;

            if (input.ResetPressed)
            {
                reset.Execute();
                return;
            }

            if (play.Phase == PlayPhase.InPossession) Aim();
            ResetIfLost();
        }

        void Aim()
        {
            PlayerView holder = findView(play.Holder);
            Vector3 direction = input.ReadDirection(holder.transform.position);
            holder.LookTowards(direction);

            var pitchDirection = PitchConversion.ToPitch(direction);
            evaluate.Execute(pitchDirection);

            if (input.PassPressed)
                pass.Execute(pitchDirection);
        }

        void ResetIfLost()
        {
            if (play.Phase != PlayPhase.PassLost)
            {
                timeSinceLoss = 0f;
                return;
            }

            timeSinceLoss += Time.deltaTime;
            if (timeSinceLoss >= resetDelayAfterLoss)
                reset.Execute();
        }
    }
}
