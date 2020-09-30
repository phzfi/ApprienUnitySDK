using UnityEngine;
using UnityEngine.UI;

namespace ApprienUnitySDK.ExampleProject
{
    public class ExampleStoreDebug_UI : MonoBehaviour
    {
        [Space]
        [SerializeField] private Text _apprienStatusText = default;
        [SerializeField] private Text _messageText = default;

        public void DebugApprienStatus(string message, Color messageColor)
        {
            _apprienStatusText.text = message;
            _apprienStatusText.color = messageColor;
        }

        public void DebugMessage(string message, Color messageColor)
        {
            _messageText.text = message;
            _messageText.color = messageColor;
        }
    }
}