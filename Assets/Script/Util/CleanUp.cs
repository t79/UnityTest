using UnityEngine;
using System.Collections;


public class CleanUp : MonoBehaviour {

	// Safe Destroying of gameObject

	public static T SafeDestroy<T>(T obj) where T : Object {
		if (Application.isEditor) {
			Object.DestroyImmediate (obj);
		} else {
			Object.Destroy (obj);
		}
		return null;
	}

	public static T SafeDestroyGameObject<T>(T com) where T : Component {
		if (com != null) {
			SafeDestroy (com.gameObject);
		}
		return null;
	}

}
