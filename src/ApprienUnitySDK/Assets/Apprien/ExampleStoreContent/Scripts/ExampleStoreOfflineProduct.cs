using System;
using UnityEngine;

namespace ApprienUnitySDK.ExampleProject
{
    [Serializable]
    public class ExampleStoreOfflineProduct
    {
        [SerializeField] private string _nameID;
        [SerializeField] private string _price;

        public string NameID { get => _nameID; }
        public string Price { get => _price; }
    }
}