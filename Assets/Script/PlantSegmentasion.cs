using UnityEngine;
using System.Collections;
using System.IO;
using OpenCvSharp;

public class PlantSegmentasion : MonoBehaviour {

	public string plantImageFilePath = "";
	public string plantMaskFilePath = "";

	public int reductionFactor = 4;
	public int numRotationSteps = 24;

	public string[] templetShapePath;
	public int[] templetSizes;

	public float matchingTreshold;

	public bool generateNewTempletSizes = false;

	// image matrixes
	private Mat plantImageBGR;
	private Mat plantImageGray;
	private Mat plantMask;
	private Mat plantEdges;

	public void RunSegmentasion() {

		if (!LoadImages()) {
			Debug.Log ("Loading of images failed! Segmentasion aborted");
			return;
		}

		MakeGrayscaleImage ();

		if (reductionFactor > 1) {
			ReduceSegmentasionResolution ();
		}
	
		MaskSegmentasionImage ();
		CropSegmentasionImage ();

		MakeEdgeMat ();

		if (generateNewTempletSizes) {
			GenerateNewTempletSizes (10);
		}

		showImages ();

		int maxTempletSize = CalculateMaxTempletSize ();

		TempletGenerater templetGenerator = new TempletGenerater ();

		Mat matchinResultMat = Mat.Zeros(plantEdges.Size(), plantEdges.Type());

		double minValue, maxValue;
		Point minLoc, maxLoc;

		foreach (string path in templetShapePath) {

			if (!templetGenerator.LoadShape (path)) {
				Debug.Log ("Could not generate templet from: " + path);
				continue;
			}

			foreach (int templetSize in templetSizes) {

				if (templetSize > maxTempletSize) {
					continue;
				}

				templetGenerator.SetSize (templetSize);

				for (int rotStep = 0; rotStep < numRotationSteps; ++rotStep) {

					templetGenerator.SetRotasionStep (rotStep);

					if (templetGenerator.toSmallMatchingArea ()) {
						continue;
					}

					Mat matchingAreaMat = new Mat (plantEdges, templetGenerator.GetMatchingRect ());
					Mat templet = templetGenerator.GetTempletMat ();

					OpenCvSharp.Rect matchingResultRect = new OpenCvSharp.Rect (new Point (0, 0), templetGenerator.GetMatchingResultSize ());
					Mat matchingResultSubMat = new Mat (matchinResultMat, matchingResultRect);
					matchingResultSubMat.SetTo (new Scalar(0), null);

					Cv2.MatchTemplate (matchingAreaMat, templet, matchingResultSubMat, TemplateMatchModes.CCorrNormed);

					Cv2.MinMaxLoc (matchingResultSubMat, out minValue, out maxValue, out minLoc, out maxLoc);

					if (maxValue > matchingTreshold) {

					}
				}
			}
		}
	}

	private bool LoadImages() {

		// Checking that the files exist.
		if (!File.Exists (plantImageFilePath) || !File.Exists(plantMaskFilePath)) {
			Debug.Log ("Image or mask File do not exist!");
			return false;
		}

		plantImageBGR = Cv2.ImRead (plantImageFilePath, ImreadModes.Color);
		plantMask = Cv2.ImRead (plantMaskFilePath, ImreadModes.GrayScale);

		if (plantImageBGR.Empty()) {
			Debug.Log ("No readable plant image file: " + plantImageFilePath);
			return false;
		}

		if (plantMask.Empty()) {
			Debug.Log ("No readable plant mask file: " + plantMaskFilePath);
			return false;
		}

		return true;
	}

	private void MakeGrayscaleImage() {

//		if (plantImageBGR == null || plantImageBGR.Empty ()) {
//			Debug.Log ("Grayscale image not generated, error with the color image");
//			return;
//		}

		Mat plantImageLAB = plantImageBGR.CvtColor (ColorConversionCodes.BGR2Lab);

		Mat[] plantLabChannels = Cv2.Split (plantImageLAB);
		plantImageGray = Cv2.Abs (plantLabChannels [1] - plantLabChannels [2]);

	}

	private void ReduceSegmentasionResolution() {
		float scale = 1.0f / reductionFactor;
		Cv2.Resize (plantImageGray, plantImageGray, new Size (0, 0), scale, scale, InterpolationFlags.Linear); 
	}

	private void MaskSegmentasionImage() {

	}

	private void CropSegmentasionImage() {

	}

	private void MakeEdgeMat() {

	}

	private void GenerateNewTempletSizes(int numTemplets) {

	}

	private int CalculateMaxTempletSize() {
		return 0;
	}

	private void showImages() {

		Cv2.NamedWindow ("Color image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Mask image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Gray image", WindowMode.KeepRatio);

		Cv2.ImShow ("Color image", plantImageBGR);
		Cv2.ImShow ("Mask image", plantMask);
		Cv2.ImShow ("Gray image", plantImageGray);
	}
}

class TempletGenerater {

	public bool LoadShape(string shapePath) {
		return false;
	}

	public void SetSize(int size) {

	}

	public void SetRotasionStep(int rotStep) {

	}
	
	public OpenCvSharp.Rect GetMatchingRect() {
		return new OpenCvSharp.Rect ();
	}

	public Mat GetTempletMat() {
		return new Mat ();
	}

	public bool toSmallMatchingArea () {
		return false;
	}

	public Size GetMatchingResultSize () {
		return new Size (0, 0);
	}

}
