using UnityEngine;
using UnityEngine.UI;

namespace HelicopterCombat.UI
{
    [DisallowMultipleComponent]
    public sealed class EndScreenController : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        public void Configure(Text configuredTitleText, Text configuredDescriptionText)
        {
            titleText = configuredTitleText;
            descriptionText = configuredDescriptionText;
        }

        public void ShowVictoryText()
        {
            if (titleText != null)
            {
                titleText.text = "MISSION COMPLETE";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "All enemy helicopters and tanks have been destroyed.";
            }
        }

        public void ShowGameOverText()
        {
            if (titleText != null)
            {
                titleText.text = "MISSION FAILED";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "Your helicopter was destroyed.";
            }
        }
    }
}
