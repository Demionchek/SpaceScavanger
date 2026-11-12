using UnityEngine;

namespace Game.UI
{
    // Вид окон, которые собираются кодом. Спрайты лежат на UiInstaller, а не в
    // сцене: ассет Unity переимпортирует сама, стоит файлу измениться, а открытую
    // сцену — нет, и правка её файла доезжает до игры только после перезагрузки.
    public sealed class WindowSkin
    {
        public WindowSkin(Sprite frame, Sprite field, float pixelsPerUnit)
        {
            Frame = frame;
            Field = field;
            PixelsPerUnit = pixelsPerUnit;
        }

        public Sprite Frame { get; }
        public Sprite Field { get; }
        public float PixelsPerUnit { get; }
    }
}
