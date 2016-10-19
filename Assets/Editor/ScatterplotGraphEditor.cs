using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof (ScatterplotGraph))]
public class ScatterplotGraphEditor : Editor {

	private ScatterplotGraph scatterplot;

	private void OnEnable() {
		scatterplot = (ScatterplotGraph)target;
	}

	public override void OnInspectorGUI() {

		if (scatterplot == null) {
			return;
		}

		if (GUI.changed) {
			EditorUtility.SetDirty (scatterplot);
		}

		base.DrawDefaultInspector ();

		if (GUILayout.Button ("set scatterplot")) {
			scatterplot.setScatterplot();
		}

	}

}
