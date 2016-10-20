using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class ScatterplotGraph : MonoBehaviour {

	public ScatterPoint pointPrefab;
	public GridLayoutGroup plotView;

	public int numCollums = 2;
	public int numRows = 2;

	private Color trueColor = new Vector4 (1.0f, 1.0f, 1.0f, 1.0f);
	private Color falseColor = new Vector4 (1.0f, 1.0f, 1.0f, 0.4f);

	private int trueSize = 20;
	private int falseSize = 5;

	private List<ScatterPoint> points = new List<ScatterPoint> ();

	void Start() {
		setScatterplot ();
	}

	public void setScatterplot() {
		float[] testValues = {0, 0, 1, 1, 1, 0, 1 };
		setScatterplot (testValues);
	}

	public void setScatterplot(float[] values) {

		{ // Destroy the old points in the Scatterplot.
			points.Clear ();
			ScatterPoint[] sPoints = plotView.GetComponentsInChildren<ScatterPoint> ();
			for (int i = 0; i < sPoints.Length; ++i) {
				sPoints [i] = CleanUp.SafeDestroyGameObject (sPoints [i]);
			}
		}

		RectTransform rt = plotView.GetComponent<RectTransform> ();
		Vector2 cellSize = new Vector2 (rt.rect.width / numCollums, rt.rect.height / numRows);
		plotView.constraintCount = numCollums;
		plotView.cellSize = cellSize;

		trueSize = (int)(Mathf.Min (cellSize.x, cellSize.y) * 0.7);
		int maxPoints = Mathf.Min (values.Length, numCollums * numRows);

		for (int i = 0; i < maxPoints; ++i) {
			ScatterPoint newPoint = Instantiate (pointPrefab) as ScatterPoint;
			newPoint.transform.SetParent (plotView.transform);
			newPoint.name = String.Format ("point_x{0:D2}y{1:D2}", i % numCollums, i / numCollums);
			if (values [i] == 0) {
				newPoint.setPoint (falseSize, falseColor);
			} else {
				newPoint.setPoint (trueSize, trueColor);
			}
			points.Add (newPoint);
		}
	}

}
