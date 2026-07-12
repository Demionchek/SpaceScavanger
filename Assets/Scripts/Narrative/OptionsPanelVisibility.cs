using UnityEngine;

namespace Game.Narrative
{
    // Обход бага Yarn Spinner: OptionsPresenter при выключенном Use Fade Effect
    // никогда не поднимает alpha своей CanvasGroup обратно в 1 (ставит 0 на старте
    // диалога, а 1 выставляется только внутри фейда). Презентер включает
    // interactable ровно на время показа вариантов — синхронизируем alpha с ним.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class OptionsPanelVisibility : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            _canvasGroup.alpha = _canvasGroup.interactable ? 1f : 0f;
        }
    }
}
