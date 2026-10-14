using System;
using System.Collections.Generic;
using System.Linq;
using AssistedPass.Application;
using AssistedPass.Domain;
using AssistedPass.Presentation;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Composition root of the side-by-side scene: one identical pitch per assist level.
    /// Each side gets its own Play (its players, its ball, its profile) and its own visualization;
    /// a single ComparisonController drives all of them with the same input.
    /// </summary>
    [DefaultExecutionOrder(-50)] // after PlayerView.Awake (which creates the models)
    public class ComparisonBootstrap : MonoBehaviour
    {
        [Serializable]
        class Side
        {
            public AssistProfile profile;
            [Tooltip("Parent of this side's players (only these take part in its Play).")]
            public Transform playersRoot;
            public PlayerView initialHolder;
            public BallView ball;
            public Camera camera;
            public PassVisualizer visualizer;
            public DemoCamera demoCamera;
        }

        [SerializeField] Side[] sides = new Side[2];
        [SerializeField] ComparisonController controller;

        void Awake()
        {
            var lanes = new List<ComparisonController.Lane>();

            foreach (Side side in sides)
            {
                PlayerView[] views = side.playersRoot.GetComponentsInChildren<PlayerView>();
                var viewOf = views.ToDictionary(v => v.Model);
                PlayerView FindView(Player player) => viewOf[player];

                var play = new Play(views.Select(v => v.Model), side.initialHolder.Model, side.profile.CreateConfig());
                var evaluate = new EvaluatePass(play);
                var pass = new ExecutePass(play, evaluate, side.ball);
                var reset = new ResetPlay(play, side.ball);

                side.ball.Initialize(FindView);
                side.visualizer.Initialize(play);
                if (side.demoCamera != null) side.demoCamera.Initialize(play);

                lanes.Add(new ComparisonController.Lane(play, evaluate, pass, reset, FindView, side.camera));
            }

            controller.Initialize(lanes);
        }

        void Start() => controller.ResetAll(); // every ball at its initial holder's feet
    }
}
