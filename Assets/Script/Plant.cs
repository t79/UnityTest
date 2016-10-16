using UnityEngine;
using System.Collections;

public class Plant : MonoBehaviour {

	[Header("Plant file paths:")]
	[Tooltip("File path to the plant image")]
	public string plantImagePath = "";
	[Tooltip("File path to the plant mask image")]
	public string plantMaskPath = "";


	[Header("Plant properties:")]
	[Tooltip("The center point of the plant in the plant image coordinate.")]
	public Vector2 plantCenter;

}
