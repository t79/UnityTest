using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScatterPoint : MonoBehaviour {

	public Image point;

	public void setPoint(int size, Color color) {
		RectTransform rt = point.GetComponent<RectTransform> ();
		rt.sizeDelta = new Vector2 (size, size);
		point.color = color;
	}
}
