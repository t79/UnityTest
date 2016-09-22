using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof (OpenCvTest))]
public class OpenCvTestEditor : Editor {

	public override void OnInspectorGUI() {

		EditorUtility.SetDirty (target);
		OpenCvTest oct;
		oct = target as OpenCvTest;

		if (oct == null) {
			return;
		}

		base.DrawDefaultInspector ();

		if (GUILayout.Button ("Run Script")) {
			oct.RunScript ();
		}

	}
}
