using System;
using UnityEngine;

namespace ApprienUnitySDK.ExampleProject
{
    [Serializable]
    public class ExampleStoreOfflineProduct
    {
        [SerializeField] private string _nameID = default;
        [SerializeField] private string _price = default;

        public string NameID { get => _nameID; }
        public string Price { get => _price; }
    }
}