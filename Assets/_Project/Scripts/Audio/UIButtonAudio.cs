using UnityEngine;
using UnityEngine.EventSystems;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip confirmClip;
        [SerializeField] private bool playConfirmOnClick;

        public void Configure(AudioClip configuredHoverClip, AudioClip configuredClickClip, AudioClip configuredConfirmClip, bool configuredPlayConfirmOnClick)
        {
            hoverClip = configuredHoverClip;
            clickClip = configuredClickClip;
            confirmClip = configuredConfirmClip;
            playConfirmOnClick = configuredPlayConfirmOnClick;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioOneShotService.Instance?.Play2D(hoverClip, AudioCategory.UI, 0.35f, 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioOneShotService.Instance?.Play2D(clickClip, AudioCategory.UI, 0.55f, 1f);

            if (playConfirmOnClick)
            {
                AudioOneShotService.Instance?.Play2D(confirmClip, AudioCategory.UI, 0.50f, 1f);
            }
        }
    }
}
