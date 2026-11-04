using System.Text;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Имена клавиш для подсказок. Берутся из путей привязок, а не из
    // InputControl.displayName: тот приходит из раскладки ОС, и на русской
    // клавиатуре в интерфейс попало бы "У" вместо "E".
    public static class InputKeyLabels
    {
        // Только первая привязка: подсказке над объектом хватает одной клавиши.
        public static string First(InputAction action)
        {
            if (action == null)
            {
                return string.Empty;
            }

            foreach (var binding in action.bindings)
            {
                var label = FromPath(binding.effectivePath);
                if (label.Length > 0)
                {
                    return label;
                }
            }

            return string.Empty;
        }

        public static string For(InputAction action)
        {
            if (action == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var binding in action.bindings)
            {
                if (binding.isComposite)
                {
                    continue;
                }

                var label = FromPath(binding.effectivePath);
                if (label.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" / ");
                }

                builder.Append(label);
            }

            return builder.ToString();
        }

        // "<Keyboard>/upArrow" -> "Up Arrow", "<Keyboard>/e" -> "E".
        private static string FromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var slash = path.LastIndexOf('/');
            if (slash < 0 || slash == path.Length - 1)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            for (var i = slash + 1; i < path.Length; i++)
            {
                var symbol = path[i];

                if (builder.Length == 0)
                {
                    builder.Append(char.ToUpperInvariant(symbol));
                    continue;
                }

                if (char.IsUpper(symbol))
                {
                    builder.Append(' ');
                }

                builder.Append(symbol);
            }

            return builder.ToString();
        }
    }
}
