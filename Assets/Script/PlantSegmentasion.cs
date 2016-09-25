using UnityEngine;
using System.Collections;
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

					Mat matchingAreaMat = new Mat (PlantEdges, templetGenerator.GetMatchingRect ());
					Mat templet = templetGenerator.GetTempletMat ();

					OpenCvSharp.Rect matchingResultRect = new OpenCvSharp.Rect (new Point (0, 0), templetGenerator.GetMatchingResultSize ());
					Mat matchingResultSubMat = new Mat (matchinResultMat, matchingResultRect);
					matchingResultSubMat.SetTo (new Scalar (0));

					Cv2.MatchTemplate (matchingAreaMat, templet, matchingResultSubMat, TemplateMatchModes.CCorrNormed);

					Cv2.MinMaxLoc (matchingResultSubMat, out minValue, out maxValue, out minLoc, out maxLoc);

					if (maxValue > matchingTreshold) {

					}
				}
			}
		}
	}

	private bool LoadImages() {
		return false;
	}

	private void MakeGrayscaleImage() {

	}

	private void ReduceSegmentasionResolution() {

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
	
	}

	public Mat GetTempletMat() {

	}

	public bool toSmallMatchingArea () {

	}

	public Size GetMatchingResultSize () {

	}

}
