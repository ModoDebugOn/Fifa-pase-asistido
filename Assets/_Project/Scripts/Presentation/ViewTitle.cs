using AssistedPass.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AssistedPass.Presentation
{
    /// <summary>
    /// Title at the top of a camera's view, for split screen ("Asistencia alta" | "Manual").
    /// Optionally draws a divider line on the left edge of the view.
    /// It is anchored to the camera's viewport, so it works at any resolution.
    /// </summary>
    public class ViewTitle : MonoBehaviour
    {
        [SerializeField] Camera view;
        [Tooltip("On-screen text (in Spanish: it appears in the videos).")]
        [SerializeField] string title = "Asistencia alta";
        [SerializeField] Color color = Color.white;
        [SerializeField] int fontSize = 44;
        [Tooltip("Second view of the split screen: right half (divider on its left) " +
                 "or, in a vertical image, bottom half (divider on top).")]
        [SerializeField] bool dividerOnLeft;
        [SerializeField] float dividerWidth = 4f;

        Rect landscapeRect;
        Image divider;

        void Start()
        {
            if (view == null) return;
            landscapeRect = view.rect;
            // The canvas is drawn by this camera, so it already covers exactly its viewport.
            Canvas canvas = Ui.CreateCanvas("View Title", sortingOrder: 20, view);
            canvas.transform.SetParent(transform, false);

            RectTransform area = Ui.Rect("Viewport", canvas.transform);
            Ui.Stretch(area);

            Text text = Ui.Label("Title", area, fontSize, color, TextAnchor.UpperCenter);
            text.text = title;
            Ui.Stretch(text.rectTransform);
            text.rectTransform.offsetMax = new Vector2(0f, -20f); // a little margin from the top

            if (dividerOnLeft)
            {
                divider = Ui.Box("Divider", area, Color.white);
                divider.raycastTarget = false;
            }
        }

        // Side by side in a wide image, one above the other in a vertical one (reels).
        // Checked every frame because the recording changes the image size while playing.
        void LateUpdate()
        {
            if (view == null) return;
            RenderTexture output = view.targetTexture;
            float width = output != null ? output.width : Screen.width;
            float height = output != null ? output.height : Screen.height;
            bool vertical = width < height;

            view.rect = vertical
                ? new Rect(0f, dividerOnLeft ? 0f : 0.5f, 1f, 0.5f)
                : landscapeRect;

            if (divider == null) return;
            RectTransform r = divider.rectTransform;
            r.anchorMin = vertical ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            r.anchorMax = vertical ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            r.pivot = vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = vertical ? new Vector2(0f, dividerWidth) : new Vector2(dividerWidth, 0f);
        }
    }
}
