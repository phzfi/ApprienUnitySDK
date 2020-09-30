using UnityEngine;
using UnityEngine.UI;

namespace ApprienUnitySDK.ExampleProject
{
    public class ExampleStoreDebug_UI : MonoBehaviour
    {
        [Space]
        [Header("Settings")]
        [SerializeField] private bool _useDebugMessages = default;

        [Space]
        [Header("References")]
        [SerializeField] private Text _apprienStatusText = default;
        [SerializeField] private Text _messageText = default;

        private void Start()
        {
            _apprienStatusText.gameObject.SetActive(_useDebugMessages);
            _messageText.gameObject.SetActive(_useDebugMessages);
        }

        public void DebugApprienStatus(string message, Color color)
        {
            _apprienStatusText.text = message;
            _apprienStatusText.color = color;
        }

        public void DebugMessage(string message, Color color)
        {
            _messageText.text = message;
            _messageText.color = color;
        }
    }
}