using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using OpenCvSharp;

public class OpenCvTest : MonoBehaviour {

	public string imageFilePath = "";
	public int squareSize = 20;

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
		Cv2.NamedWindow ("Image2", WindowMode.KeepRatio);
		Cv2.NamedWindow ("subImage", WindowMode.KeepRatio);
		Cv2.ImShow ("Image", image);

//		OpenCvSharp.Rect roi = new OpenCvSharp.Rect (image.Width / 4, image.Height / 4, image.Width / 2, image.Height / 2);
//		Mat subImage = new Mat (image, roi);
//		//subImage = new Scalar (255, 255, 255) - subImage;
//		Cv2.Add (subImage, subImage, subImage);
//		Cv2.ImShow ("subImage", subImage);
//		Cv2.ImShow ("Image2", image);

		Mat imageGray = image.CvtColor (ColorConversionCodes.BGR2GRAY) / 10;

		// NB! EmptyClone do not overwrite memory, old contntent in memory makes glitch's
		//Mat canvas = imageGray.EmptyClone ();    
		Mat canvas = Mat.Zeros(imageGray.Size(), imageGray.Type());



		Vector2 drawingCanvasSize = new Vector2 (canvas.Width - squareSize, canvas.Height - squareSize);

		for (int i = 0; i < 30000; ++i) {

			OpenCvSharp.Rect drawingRegion = new OpenCvSharp.Rect (
				                                (int)Random.Range (0, drawingCanvasSize.x - 1), 
				                                (int)Random.Range (0, drawingCanvasSize.y - 1), 
				                                squareSize, squareSize);

			Mat drawingCanvas = new Mat (canvas, drawingRegion);
			Mat drawingSource = new Mat (imageGray, drawingRegion);

			Cv2.Add (drawingCanvas, drawingSource, drawingCanvas);

			Cv2.ImShow ("Image2", canvas);
		}

	}

}
