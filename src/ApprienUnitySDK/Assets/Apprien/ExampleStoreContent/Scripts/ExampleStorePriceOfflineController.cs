using UnityEngine;

namespace ApprienUnitySDK.ExampleProject
{
    public class ExampleStorePriceOfflineController : MonoBehaviour
    {
        [SerializeField] private ExampleStoreOfflineProduct[] _standardIAPs = new ExampleStoreOfflineProduct[3];
        [SerializeField] private ExampleStoreOfflineProduct[] _apprienIAPs = new ExampleStoreOfflineProduct[3];
        [SerializeField] private ExampleStoreOfflineProduct[] _standardSubscriptions = new ExampleStoreOfflineProduct[3];
        [SerializeField] private ExampleStoreOfflineProduct[] _apprienSubscriptions = new ExampleStoreOfflineProduct[3];

        public ExampleStoreOfflineProduct[] StandardIAPs { get => _standardIAPs; }
        public ExampleStoreOfflineProduct[] StandardSubscriptions { get => _standardSubscriptions; }
        public ExampleStoreOfflineProduct[] ApprienIAPs { get => _apprienIAPs; }
        public ExampleStoreOfflineProduct[] ApprienSubscriptions { get => _apprienSubscriptions; }
    }
}