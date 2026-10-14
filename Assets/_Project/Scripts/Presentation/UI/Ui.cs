using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AssistedPass.Presentation.UI
{
    /// <summary>
    /// Builds the on-screen UI in code, with a flat style that reads well on video.
    /// It uses uGUI (Canvas) instead of OnGUI, and every canvas is drawn BY a camera
    /// (Screen Space - Camera): that way the UI is part of what the camera renders, so it also
    /// appears when the recording captures the cameras into a texture, and in split screen
    /// each canvas covers exactly its camera's half.
    /// </summary>
    public static class Ui
    {
        static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Canvas drawn by that camera, covering its viewport. Its units are pixels.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, Camera camera)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            // Right in front of the near plane, so nothing on the pitch can cover it.
            canvas.planeDistance = camera.nearClipPlane + 0.05f;
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }

        /// <summary>The buttons and the slider need an EventSystem; created once if missing.</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Box(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>Text with a dark outline, so it can be read over the grass and over the cone.</summary>
        public static Text Label(string name, Transform parent, int size, Color color, TextAnchor alignment)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public static Button Button(string name, Transform parent, string caption, int size)
        {
            // The tint multiplies the image color: the image is white and the tint sets the alpha.
            Image background = Box(name, parent, Color.white);
            var button = background.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.15f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.3f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.15f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;

            Text text = Label("Caption", background.transform, size, Color.white, TextAnchor.MiddleCenter);
            text.text = caption;
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Horizontal slider: track, fill (its color can change) and handle.</summary>
        public static Slider Slider(string name, Transform parent, out Image fill)
        {
            RectTransform root = Rect(name, parent);
            Image track = Box("Track", root, new Color(1f, 1f, 1f, 0.2f));
            Stretch(track.rectTransform, 0f, 0.3f, 1f, 0.7f);

            RectTransform fillArea = Rect("Fill Area", root);
            Stretch(fillArea, 0f, 0.3f, 1f, 0.7f);
            fill = Box("Fill", fillArea, Color.white);
            Stretch(fill.rectTransform);

            RectTransform handleArea = Rect("Handle Area", root);
            Stretch(handleArea);
            Image handle = Box("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(14f, 0f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        /// <summary>Checkbox with its caption on the right.</summary>
        public static Toggle Toggle(string name, Transform parent, string caption, int size)
        {
            RectTransform root = Rect(name, parent);
            // Dark box with a light border; the check inside is cyan, like the cone.
            Image box = Box("Box", root, new Color(0f, 0f, 0f, 0.5f));
            box.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.6f);
            box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.sizeDelta = new Vector2(size, size);
            box.rectTransform.anchoredPosition = new Vector2(size * 0.5f, 0f);

            Image check = Box("Check", box.transform, new Color(0.1f, 0.85f, 1f));
            Stretch(check.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);

            Text text = Label("Caption", root, size, Color.white, TextAnchor.MiddleLeft);
            text.text = caption;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(size * 1.6f, 0f);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            return toggle;
        }

        /// <summary>Fills the parent (or the given normalized area of it).</summary>
        public static void Stretch(RectTransform rect, float minX = 0f, float minY = 0f, float maxX = 1f, float maxY = 1f)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
