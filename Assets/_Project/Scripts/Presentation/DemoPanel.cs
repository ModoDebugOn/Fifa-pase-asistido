using System.Collections.Generic;
using AssistedPass.Application;
using AssistedPass.Domain;
using AssistedPass.Presentation.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AssistedPass.Presentation
{
    /// <summary>
    /// On-screen panel for the demo: assistance level slider, profile switch
    /// and visualization layer toggles. H hides/shows the panel (to record a clean shot).
    /// The on-screen texts are in Spanish because they appear in the channel's videos.
    /// </summary>
    public class DemoPanel : MonoBehaviour
    {
        [SerializeField] PassVisualizer visualizer;
        [SerializeField] bool visible = true;

        Play play;
        ChangeAssist changeAssist;
        IReadOnlyList<(string name, AssistConfig config)> profiles;

        Canvas canvas;
        Text title;
        Slider slider;
        Image sliderFill;
        readonly List<(Toggle toggle, System.Func<bool> get)> layerToggles = new List<(Toggle, System.Func<bool>)>();

        /// <param name="play">Read only: to show the current level.</param>
        /// <param name="changeAssist">Every change goes through this use case.</param>
        /// <param name="profiles">Profiles for the buttons.</param>
        /// <summary>Same as pressing H.</summary>
        public bool Visible { get => visible; set => visible = value; }

        public void Initialize(Play play, ChangeAssist changeAssist,
                               IReadOnlyList<(string name, AssistConfig config)> profiles)
        {
            this.play = play;
            this.changeAssist = changeAssist;
            this.profiles = profiles;
        }

        void Start()
        {
            if (play == null) return;
            Ui.EnsureEventSystem();
            Build();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) visible = !visible;
        }

        // The panel only shows the state: if it changes elsewhere (keys 1-5, profile buttons),
        // the controls follow it without firing their events again.
        void LateUpdate()
        {
            if (canvas == null) return;
            canvas.enabled = visible;
            if (!visible) return;

            AssistConfig config = play.Config;
            title.text = $"Asistencia: {config.Level:0.00}   (cono ±{config.HalfAngle:0}°)";
            slider.SetValueWithoutNotify(config.Level);
            if (visualizer != null)
                sliderFill.color = visualizer.ConeColor(config.Level);

            foreach (var (toggle, get) in layerToggles) toggle.SetIsOnWithoutNotify(get());
        }

        // ------------------------------------------------------------------ construction

        void Build()
        {
            canvas = Ui.CreateCanvas("Demo Panel", sortingOrder: 30, Camera.main);
            canvas.transform.SetParent(transform, false);

            // Panel at the top-left corner; its height adapts to the content.
            Image panel = Ui.Box("Panel", canvas.transform, new Color(0f, 0f, 0f, 0.6f));
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(440f, 0f);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            title = Ui.Label("Title", panel.transform, 26, Color.white, TextAnchor.MiddleLeft);
            Height(title.gameObject, 34f);

            slider = Ui.Slider("Level", panel.transform, out sliderFill);
            slider.onValueChanged.AddListener(changeAssist.SetLevel);
            Height(slider.gameObject, 30f);

            RectTransform buttons = Ui.Rect("Profiles", panel.transform);
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childForceExpandWidth = true;
            row.childControlWidth = true;
            row.childControlHeight = true;
            Height(buttons.gameObject, 40f);
            foreach (var (name, profile) in profiles)
            {
                AssistConfig chosen = profile;
                Ui.Button(name, buttons, name, 22).onClick.AddListener(() => changeAssist.UseProfile(chosen));
            }

            if (visualizer != null)
            {
                LayerToggle(" 1 · Cono de asistencia", () => visualizer.showCone, v => visualizer.showCone = v);
                LayerToggle(" 2 · Dirección del input", () => visualizer.showDirection, v => visualizer.showDirection = v);
                LayerToggle(" 3 · Candidatos", () => visualizer.showCandidates, v => visualizer.showCandidates = v);
                LayerToggle(" 4 · Puntuaciones", () => visualizer.showScores, v => visualizer.showScores = v);
                LayerToggle(" 5 · Receptor elegido", () => visualizer.showChosen, v => visualizer.showChosen = v);
            }

            Text help = Ui.Label("Help", panel.transform, 18, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft);
            help.text = "Ratón/stick: apuntar · Espacio/A: pasar\nR/Y: reiniciar · C: cámara · H: ocultar panel";
            Height(help.gameObject, 48f);
        }

        void LayerToggle(string caption, System.Func<bool> get, System.Action<bool> set)
        {
            Toggle toggle = Ui.Toggle(caption, canvas.transform.GetChild(0), caption, 22);
            toggle.onValueChanged.AddListener(v => set(v));
            Height(toggle.gameObject, 28f);
            layerToggles.Add((toggle, get));
        }

        static void Height(GameObject go, float height) =>
            go.AddComponent<LayoutElement>().preferredHeight = height;
    }
}
