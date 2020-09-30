using System;
using UnityEngine;

namespace ApprienUnitySDK.ExampleProject
{
    [Serializable]
    public class ExampleStoreOfflineProduct
    {
        [SerializeField] private string _name = default;
        [SerializeField] private string _price = default;
    }
}