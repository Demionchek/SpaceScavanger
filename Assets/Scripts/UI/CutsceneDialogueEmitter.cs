using UnityEngine;

namespace Game.UI
{
    // Живёт в сцене катсцены. Timeline Signal вызывает Play(node) — контроллер
    // ставит таймлайн на паузу на время реплики и снимает по её завершении.
    public sealed class CutsceneDialogueEmitter : MonoBehaviour
    {
        private CutsceneController _controller;

        public void Bind(CutsceneController controller)
        {
            _controller = controller;
        }

        public void Play(string node)
        {
            if (_controller != null)
            {
                _controller.PlayDialogue(node);
            }
        }

        public void Finish()
        {
            if (_controller != null)
            {
                _controller.Finish();
            }
        }
    }
}
