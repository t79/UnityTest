using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof (PlantSegmentasion))]
public class PlantSegmentasionEditor : Editor {

	public override void OnInspectorGUI() {

		EditorUtility.SetDirty (target);
		PlantSegmentasion ps;
		ps = target as PlantSegmentasion;

		if (ps == null) {
			return;
		}

		base.DrawDefaultInspector ();

		if (GUILayout.Button ("Run Segmentasion")) {
			ps.RunSegmentasion ();
		}

	}
}
