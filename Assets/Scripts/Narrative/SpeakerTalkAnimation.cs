using UnityEngine;

namespace Game.Narrative
{
    // Вешается на talk_anim над головой персонажа. Объект держим выключенным:
    // SpeakerTalkPresenter включает его на время реплик этого говорящего.
    public sealed class SpeakerTalkAnimation : MonoBehaviour
    {
        [Tooltip("Имя говорящего из Yarn-строки (\"Officer: ...\"). Пусто — имя родителя.")]
        [SerializeField] private string _speaker;

        public string Speaker
        {
            get
            {
                if (!string.IsNullOrEmpty(_speaker))
                {
                    return _speaker;
                }

                return transform.parent != null ? transform.parent.name : name;
            }
        }

        public void SetTalking(bool talking)
        {
            gameObject.SetActive(talking);
        }
    }
}
