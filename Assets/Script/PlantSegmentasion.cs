using UnityEngine;
using System.Collections;
using System.IO;
using OpenCvSharp;

public class PlantSegmentasion : MonoBehaviour {

	public string plantImageFilePath = "";
	public string plantMaskFilePath = "";

	public int reductionFactor = 4;
	public float plantPadding = 0.2f;
	public int numRotationSteps = 24;

	public string[] templetShapePath;
	public int[] templetSizes;

	public float matchingTreshold;

	public bool generateNewTempletSizes = false;

	// image matrixes
	private Mat plantImageBGR;
	private Mat plantSegmentasionImage;
	private Mat plantMask;
	private Mat plantEdges;

	private OpenCvSharp.Rect plantBounds;

	public void RunSegmentasion() {

		if (!LoadImages()) {
			Debug.Log ("Loading of images failed! Segmentasion aborted");
			return;
		}

		MakeSegmentasionImage ();

		if (reductionFactor > 1) {
			ReduceSegmentasionResolution ();
		}
	
		MaskSegmentasionImage ();
		FindPlantBounds ();
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

	private void MakeSegmentasionImage() {

//		if (plantImageBGR == null || plantImageBGR.Empty ()) {
//			Debug.Log ("Grayscale image not generated, error with the color image");
//			return;
//		}

		Mat plantImageLAB = plantImageBGR.CvtColor (ColorConversionCodes.BGR2Lab);

		Mat[] plantLabChannels = Cv2.Split (plantImageLAB);
		plantSegmentasionImage = Cv2.Abs (plantLabChannels [1] - plantLabChannels [2]);

	}

	private void ReduceSegmentasionResolution() {
		float scale = 1.0f / reductionFactor;
		Cv2.Resize (plantSegmentasionImage, plantSegmentasionImage, new Size (0, 0), scale, scale, InterpolationFlags.Linear); 
	}

	private void MaskSegmentasionImage() {
		if (plantMask.Size() != plantSegmentasionImage.Size()) {
			Cv2.Resize (plantMask, plantMask, plantSegmentasionImage.Size (), 0, 0, InterpolationFlags.Linear);
		}
		Cv2.BitwiseAnd (plantSegmentasionImage, plantMask, plantSegmentasionImage);
	}

	private void FindPlantBounds() {
		Mat nonZero = new Mat ();
		Cv2.FindNonZero (plantSegmentasionImage, nonZero);
		plantBounds = Cv2.BoundingRect (nonZero);
	}

	private void CropSegmentasionImage() {

		int roiX = plantBounds.X - (int)(plantBounds.Width * plantPadding);
		if (roiX < 0) {
			roiX = 0;
		}
		int roiY = plantBounds.Y - (int)(plantBounds.Height * plantPadding);
		if (roiY < 0) {
			roiY = 0;
		}
		int roiWidth = plantBounds.Width + (int)(plantBounds.Width * plantPadding * 2);
		if (roiWidth + roiX > plantSegmentasionImage.Width) {
			roiWidth = plantSegmentasionImage.Width - roiX;
		}
		int roiHeight = plantBounds.Height + (int)(plantBounds.Height * plantPadding * 2);
		if (roiHeight + roiY > plantSegmentasionImage.Height) {
			roiHeight = plantSegmentasionImage.Height - roiY;
		}

		OpenCvSharp.Rect roi = new OpenCvSharp.Rect (roiX, roiY, roiWidth, roiHeight);

		plantSegmentasionImage = new Mat (plantSegmentasionImage, roi);
	}

	private void MakeEdgeMat() {

		Mat sobelX = new Mat ();
		Cv2.Sobel (plantSegmentasionImage, sobelX, MatType.CV_16S, 1, 0, 3);
		Cv2.ConvertScaleAbs (sobelX, sobelX);

		Mat sobelY = new Mat ();
		Cv2.Sobel (plantSegmentasionImage, sobelY, MatType.CV_16S, 0, 1, 3);
		Cv2.ConvertScaleAbs (sobelY, sobelY);

		plantEdges = new Mat (); //Mat.Zeros(plantSegmentasionImage.Size(), plantSegmentasionImage.Type());
		Cv2.AddWeighted (sobelX, 0.5, sobelY, 0.5, 0, plantEdges);
	}

	private void GenerateNewTempletSizes(int numTemplets) {

	}

	private int CalculateMaxTempletSize() {
		return 0;
	}

	private void showImages() {

		Cv2.NamedWindow ("Color image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Mask image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Segmentasion image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Edge image", WindowMode.KeepRatio);

		Cv2.ImShow ("Color image", plantImageBGR);
		Cv2.ImShow ("Mask image", plantMask);
		Cv2.ImShow ("Segmentasion image", plantSegmentasionImage);
		Cv2.ImShow ("Edge image", plantEdges);
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
