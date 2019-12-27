using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu]
public class ApprienConnection : ScriptableObject
{
	[Header("Apprien Integration Properties", order = 0)]
	[Header("Authentication", order = 1)]
	[Tooltip("Apprien authentication token. Retrieved from the Apprien Dashboard per game.")]
	[SerializeField]
	public string Token;
}