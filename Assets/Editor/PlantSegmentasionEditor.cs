using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof (PlantSegmentasion))]
public class PlantSegmentasionEditor : Editor {

	private PlantSegmentasion ps;

	private void OnEnable() {
		ps = (PlantSegmentasion)target;
	}

	public override void OnInspectorGUI() {

		if (ps == null) {
			return;
		}

		if (GUI.changed) {
			EditorUtility.SetDirty (ps);
		}

		base.DrawDefaultInspector ();

		if (GUILayout.Button ("Run Segmentasion")) {
			ps.RunSegmentasion ();
		}

	}
}
