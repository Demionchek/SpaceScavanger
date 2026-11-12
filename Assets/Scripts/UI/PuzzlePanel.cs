using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    // Общая рама двух мини-игр. Окно носит тот же спрайт, что и остальные окна
    // игры (`info_0005_window1`), девятислойкой — поэтому размер панели свободный,
    // а вот текст держится внутри `FrameInset`: у кромки спрайта сверху и снизу
    // широкая декоративная полоса, и заголовок, посаженный по краю панели, уехал
    // бы прямо на неё. Собирается кодом, как легенда управления: в сцене у окон
    // есть только шрифт и два спрайта.
    public sealed class PuzzlePanel
    {
        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private readonly TMP_Text _hint;
        private readonly TMP_Text _status;

        public RectTransform Content { get; }

        public PuzzlePanel(Transform host, in Style style)
        {
            // Окно вешается на сам канвас, а не на объект-носитель: тот в сцене пустой,
            // его прямоугольник 100x100, и затемнение накрыло бы пятачок в середине
            // экрана. Последним ребёнком канваса оно к тому же рисуется поверх всех
            // панелей HUD, где бы носитель ни лежал в иерархии.
            var canvas = host.GetComponentInParent<Canvas>();
            _root = new GameObject("PuzzleWindow", typeof(RectTransform));
            _root.transform.SetParent(canvas != null ? canvas.rootCanvas.transform : host, false);

            var rootRect = (RectTransform)_root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _root.AddComponent<Image>().color = style.Backdrop;

            var panel = Box("Panel", _root.transform, style.PanelSize, Vector2.zero);
            Plate(panel, style.Frame, style.FramePixelsPerUnit, style.FrameTint, style.PlateFallback);

            var inner = style.PanelSize * 0.5f - style.FrameInset;
            var width = inner.x * 2f;

            _title = Label(Box("Title", panel, new Vector2(width, 110f), new Vector2(0f, inner.y - 55f)),
                style.Font, 80f, style.Text);

            _hint = Label(Box("Hint", panel, new Vector2(width, 130f), new Vector2(0f, inner.y - 185f)),
                style.Font, 52f, style.Dim);
            _hint.textWrappingMode = TextWrappingModes.Normal;

            Content = Box("Content", panel, new Vector2(width, 230f), new Vector2(0f, -75f));

            _status = Label(Box("Status", panel, new Vector2(width, 90f), new Vector2(0f, -inner.y + 45f)),
                style.Font, 52f, style.Text);

            _root.SetActive(false);
        }

        public void Open(string title, string hint)
        {
            _title.text = title;
            _hint.text = hint;
            _root.SetActive(true);
        }

        public void Close() => _root.SetActive(false);

        public void SetStatus(string status) => _status.text = status;

        // Координаты, а не layout-группа: внутри мини-игры всё стоит на своих
        // местах относительно центра плиты.
        public static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);

            var rect = (RectTransform)holder.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static TMP_Text Label(RectTransform holder, TMP_FontAsset font, float size, Color color)
        {
            var label = holder.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        public static Image Plate(RectTransform holder, Color color)
        {
            var image = holder.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // Спрайт растягивается девятислойкой, а `pixelsPerUnitMultiplier` держит
        // кромку в том же масштабе, в каком её рисуют остальные окна (0.25 — арт
        // нарисован в четыре раза мельче канваса 3840x2160). Без спрайта Image
        // заливает прямоугольник белым, и окно читается как поломка, — поэтому
        // запасной цвет тёмный, а не тот же белый тинт.
        public static Image Plate(RectTransform holder, Sprite sprite, float pixelsPerUnit,
            Color tint, Color fallback)
        {
            var image = Plate(holder, sprite != null ? tint : fallback);

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = pixelsPerUnit;
            }

            return image;
        }

        public struct Style
        {
            public TMP_FontAsset Font;
            public Vector2 PanelSize;
            public Vector2 FrameInset;
            public Sprite Frame;
            public float FramePixelsPerUnit;
            public Color Backdrop;
            public Color FrameTint;
            public Color PlateFallback;
            public Color Text;
            public Color Dim;
        }
    }
}
