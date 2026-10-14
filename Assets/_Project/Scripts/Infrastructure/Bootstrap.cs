using System.Linq;
using AssistedPass.Application;
using AssistedPass.Domain;
using AssistedPass.Presentation;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Composition root: the only place where the pieces are created and wired together.
    /// Collects the players in the scene, creates the Play and the use cases,
    /// and hands them to the Unity adapters and to the presentation.
    /// </summary>
    [DefaultExecutionOrder(-50)] // after PlayerView.Awake (which creates the models)
    public class Bootstrap : MonoBehaviour
    {
        [Header("Play")]
        [SerializeField] AssistProfile profile;
        [SerializeField] PlayerView initialHolder;
        [SerializeField] BallView ball;
        [SerializeField] PlayController controller;

        [Header("Presentation")]
        [SerializeField] PassVisualizer visualizer;
        [SerializeField] DemoPanel panel;
        [Tooltip("Profiles shown as buttons on the panel.")]
        [SerializeField] AssistProfile[] panelProfiles;
        [Tooltip("Optional: lets the camera shots that align with the aim read it.")]
        [SerializeField] DemoCamera demoCamera;

        public Play Play { get; private set; }

        ResetPlay reset;

        void Awake()
        {
            PlayerView[] views = FindObjectsByType<PlayerView>();
            var viewOf = views.ToDictionary(v => v.Model);
            PlayerView FindView(Player player) => viewOf[player];

            Play = new Play(views.Select(v => v.Model), initialHolder.Model, profile.CreateConfig());

            var evaluate = new EvaluatePass(Play);
            var pass = new ExecutePass(Play, evaluate, ball);
            reset = new ResetPlay(Play, ball);

            ball.Initialize(FindView);
            controller.Initialize(Play, evaluate, pass, reset, FindView);

            visualizer.Initialize(Play);
            panel.Initialize(Play, new ChangeAssist(Play), panelProfiles
                .Select(p => (p.DisplayName, p.CreateConfig()))
                .ToList());
            if (demoCamera != null) demoCamera.Initialize(Play);
        }

        void Start() => reset.Execute(); // ball at the initial holder's feet
    }
}
