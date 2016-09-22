using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using OpenCvSharp;

public class OpenCvTest : MonoBehaviour {

	public string imageFilePath = "";

	public void RunScript() {

		// Checking that that there exist a file of any type.
		if (!File.Exists (imageFilePath)) {
			Debug.Log ("Image File do not exist!");
			//return;
		}

		Mat image = Cv2.ImRead (imageFilePath, ImreadModes.Color);

		if (image.Empty()) {
			Debug.Log ("No readable image file.");
			return;
		}

		Cv2.NamedWindow ("Image", WindowMode.KeepRatio);
		Cv2.ImShow ("Image", image);

	}

}
