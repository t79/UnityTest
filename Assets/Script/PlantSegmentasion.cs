using UnityEngine;
using System.Collections;
using System.IO;
using OpenCvSharp;

public class PlantSegmentasion : MonoBehaviour {

	// --- Public Properties 


	[Header("Plant file paths:")]
	[Tooltip("File path to the plant image")]
	public string plantImagePath = "";
	[Tooltip("File path to the plant mask image")]
	public string plantMaskPath = "";


	[Header("Plant properties:")]
	[Tooltip("The center point of the plant in the plant image coordinate.")]
	public Vector2 plantCenter;
	[Tooltip("The width of the plant center, where 1 is the diameter of the plant.")]
	[Range(0.01f, 1.0f)]
	public float plantCenterSize = 0.2f;
	[Tooltip("The width of the padding around the plant, where 1 is the diameter of the plant.")]
	[Range(0.01f, 2.0f)]
	public float plantPadding = 0.2f;
	[Tooltip("Reduces the resolution of the segmentasion.")]
	[Range(1, 10)]
	public int reductionFactor = 4;


	[Header("Matching properites:")]
	[Tooltip("How many steps when rottating the templet 360 degree.")]
	public int numRotationSteps = 24;
	[Tooltip("How many difrent sizes. Only used when regenereating the size list.")]
	public int numSizes = 10;
	[Tooltip("The maximum Templet diameter, where 1 is the diameter of the plant.")]
	[Range(0.1f, 1.0f)]
	public float maxTempletPlantRatio = 0.8f;
	[Tooltip("Resets the size list, generate linearly between 0.1 and maximum size.")]
	public bool generateSizes = false;
	[Tooltip("List of all templet sizes.")]
	[ContextMenuItem("Regenerate sizes.", "GenerateSizeValues")]
	public int[] templetSizes;
	[Tooltip("List of all the shapes file paths.")]
	public string[] templetShapePath;


	[Header("Templet properties:")]
	[Tooltip("The maximum length on leaf steam, where 1 is the diamether of the leaf shape.")]
	[Range(0.1f, 2.0f)]
	public float maxSteamLength = 1.0f;
	[Tooltip("The minimum matching value to become a leaf candidate.")]
	[Range(0.0f, 1.0f)]
	public float minMatchTreshold = 0.2f;
	[Tooltip("The maximum matching value to become a leaf candidate.")]
	[Range(0.0f, 1.0f)]
	public float maxMatchTreshold = 1.0f;
	[Tooltip("How big part of the leaf candidate can go outside of the mask.")]
	[Range(0.0f, 0.5f)]
	public float outsideMaskRatio = 0.05f;


	// --- Private Properties

	// image matrixes
	private Mat plantImageBGR;
	private Mat plantSegmentasionImage;
	private Point plantSegmentasionCenter;
	private Mat plantMask;
	private Mat plantMaskCropt;
	private Mat plantEdges;

	private Mat accumulatedLeafCandidates;

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

		if (generateSizes) {
			GenerateNewTempletSizes (10);
		}

		int maxTempletSize = CalculateMaxTempletSize ();

		TempletGenerator templetGenerator = 
			new TempletGenerator(maxTempletSize, 
					plantSegmentasionImage.Type(), 
					numRotationSteps, 
					plantSegmentasionCenter,
					(int)((plantBounds.Width > plantBounds.Height ? 
									plantBounds.Width : plantBounds.Height) * plantCenterSize),
					maxSteamLength,
					new OpenCvSharp.Rect(0, 0, plantSegmentasionImage.Width, plantSegmentasionImage.Height),
					plantMaskCropt,
					outsideMaskRatio);

		Mat matchinResultMat = Mat.Zeros(plantSegmentasionImage.Size(), plantSegmentasionImage.Type());
		accumulatedLeafCandidates = Mat.Zeros (plantSegmentasionImage.Size (), plantSegmentasionImage.Type ());

		double minValue, maxValue;
		Point minLoc, maxLoc;

		foreach (string path in templetShapePath) {

			if (!templetGenerator.LoadShape (path)) {
				Debug.Log ("Could not generate templet from: " + path);
				continue;
			}

			foreach (int templetSize in templetSizes) {

				if (templetSize > maxTempletSize) {
					Debug.Log (templetSize + " > " + maxTempletSize);
					continue;
				}

				templetGenerator.SetSize (templetSize);

				for (int rotStep = 0; rotStep < numRotationSteps; ++rotStep) {

					if (!templetGenerator.SetRotatsionStep (rotStep)) {
						Debug.Log ("Matching area is to small.");
						continue;
					}

					OpenCvSharp.Rect matchingRect = templetGenerator.GetMatchingRect ();

					Mat matchingAreaMat = new Mat (plantEdges, matchingRect);
					Mat templet = templetGenerator.GetTemplet ();

					OpenCvSharp.Rect matchingResultRect = new OpenCvSharp.Rect( 0, 0,
																matchingAreaMat.Width - templet.Width + 1,
																matchingAreaMat.Height - templet.Height + 1);
					
					Mat matchingResultSubMat = new Mat (matchinResultMat, matchingResultRect);
					matchingResultSubMat.SetTo (new Scalar(0), null);

					Cv2.MatchTemplate (matchingAreaMat, templet, matchingResultSubMat, TemplateMatchModes.CCorrNormed);

					Cv2.MinMaxLoc (matchingResultSubMat, out minValue, out maxValue, out minLoc, out maxLoc);

					if (maxValue >= minMatchTreshold && maxValue <= maxMatchTreshold) {

						if (templetGenerator.checkAgainstMask (maxLoc)) {

							OpenCvSharp.Rect drawingRect = new OpenCvSharp.Rect (maxLoc.X + matchingRect.X, 
																maxLoc.Y + matchingRect.Y, 
																templet.Width,
																templet.Height);
						
							Mat accLCDrawingArea = new Mat (accumulatedLeafCandidates, drawingRect);
							Cv2.Add (accLCDrawingArea, templet, accLCDrawingArea);
						}
					}
				}
			}
		}
			
		showImages ();
	}

	private bool LoadImages() {

		// Checking that the files exist.
		if (!File.Exists (plantImagePath) || !File.Exists(plantMaskPath)) {
			Debug.Log ("Image or mask File do not exist!");
			return false;
		}

		plantImageBGR = Cv2.ImRead (plantImagePath, ImreadModes.Color);
		plantMask = Cv2.ImRead (plantMaskPath, ImreadModes.GrayScale);

		if (plantImageBGR.Empty()) {
			Debug.Log ("No readable plant image file: " + plantImagePath);
			return false;
		}

		if (plantMask.Empty()) {
			Debug.Log ("No readable plant mask file: " + plantMaskPath);
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

		plantSegmentasionCenter = new Point (plantCenter.x, plantCenter.y);

	}

	private void ReduceSegmentasionResolution() {
		float scale = 1.0f / reductionFactor;
		Cv2.Resize (plantSegmentasionImage, 
					plantSegmentasionImage, 
					new Size (0, 0), 
					scale, 
					scale, 
					InterpolationFlags.Linear);

		plantSegmentasionCenter.X = (int)(plantSegmentasionCenter.X * scale);
		plantSegmentasionCenter.Y = (int)(plantSegmentasionCenter.Y * scale);
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
		plantMaskCropt = new Mat (plantMask, roi);

		plantSegmentasionCenter.X -= roi.X;
		plantSegmentasionCenter.Y -= roi.Y;
	}

	private void MakeEdgeMat() {

		Mat sobelX = new Mat ();
		Cv2.Sobel (plantSegmentasionImage, sobelX, MatType.CV_16S, 1, 0, 3);
		Cv2.ConvertScaleAbs (sobelX, sobelX);

		Mat sobelY = new Mat ();
		Cv2.Sobel (plantSegmentasionImage, sobelY, MatType.CV_16S, 0, 1, 3);
		Cv2.ConvertScaleAbs (sobelY, sobelY);

		plantEdges = new Mat ();
		Cv2.AddWeighted (sobelX, 0.5, sobelY, 0.5, 0, plantEdges);
	}

	private void GenerateNewTempletSizes(int numTemplets) {

	}

	private int CalculateMaxTempletSize() {
		return (int)((plantBounds.Width > plantBounds.Height ? 
							plantBounds.Width : plantBounds.Height) * maxTempletPlantRatio);
	}

	private void showImages() {

		Cv2.NamedWindow ("Color image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Mask image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Segmentasion image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Edge image", WindowMode.KeepRatio);
		Cv2.NamedWindow ("Leaf Candidates", WindowMode.KeepRatio);

		Cv2.ImShow ("Color image", plantImageBGR);
		Cv2.ImShow ("Mask image", plantMask);
		Cv2.ImShow ("Segmentasion image", plantSegmentasionImage);
		Cv2.ImShow ("Edge image", plantEdges);

		Mat leafCandidateOnPlant = new Mat ();
		Cv2.Add (accumulatedLeafCandidates, plantSegmentasionImage, leafCandidateOnPlant);

		Cv2.ImShow ("Leaf Candidates", leafCandidateOnPlant);
	}

	public void GenerateSizeValues() {
		Debug.Log ("Generate sizez, not implementet.");
	}
}

class TempletGenerator {

	private enum State : byte {
		NotSet, ShapeSet, SízeSet, TempletSet
	};

	private State generatorState;

	private Point[][] contours;
	private Point2f[] contourNormalized;
	private Point2f[] contourScaled;

	private int templetCenter;
	private RotatPoint rotatPoint;

	private int shiftDistance;
	private Point uperPlantCenterCorner;
	private float maxSteamLengthRatio;

	private Point2f[] steam;
	private Point2f[] steamNormalized;
	private Point2f[] steamScaled;

	private Mat templetContour;
	private Mat templetContureMat;
	private Mat templetFill;
	private Mat templetFillMat;

	private Point[] matchingArea;
	private OpenCvSharp.Rect matchingBoxRect;
	private OpenCvSharp.Rect maxMatchingRect;

	private Mat plantMaskInvert;
	private float outsideMaskRatio;

	public TempletGenerator(
				int maxTempletSize, 
				MatType matType,  
				int numRotationSteps, 
				Point plantCenter, 
				int plantCenterWidth,
				float maxSteamLengthRatio,
				OpenCvSharp.Rect maxMatchingRect,
				Mat plantMask,
				float outsideMaskRatio) {

		generatorState = State.NotSet;

		this.templetCenter = maxTempletSize / 2; 
		this.shiftDistance = plantCenterWidth / 2;
		this.uperPlantCenterCorner = new Point (plantCenter.X - shiftDistance, 
											plantCenter.Y - shiftDistance);
		this.maxSteamLengthRatio = maxSteamLengthRatio;
		this.maxMatchingRect = maxMatchingRect;
		this.plantMaskInvert = 255 - plantMask;
		this.outsideMaskRatio = outsideMaskRatio;

		rotatPoint = new RotatPoint (numRotationSteps);
		templetContureMat = Mat.Zeros (maxTempletSize, maxTempletSize, matType);
		templetFillMat = Mat.Zeros (maxTempletSize, maxTempletSize, matType);

		matchingArea = new Point[8];
		for (int i = 0; i < matchingArea.Length; ++i) {
			matchingArea [i] = new Point ();
		}
	}

	public bool LoadShape(string shapePath) {

		if (!File.Exists (shapePath)) {
			Debug.Log ("Shape File do not exist!");
			return false;
		}

		Mat shapeImage = Cv2.ImRead (shapePath, ImreadModes.GrayScale);

		if (shapeImage.Empty()) {
			Debug.Log ("No readable shape file: " + shapePath);
			return false;
		}

		Cv2.CopyMakeBorder (shapeImage, shapeImage, 10, 10, 10, 10, BorderTypes.Constant);

		shapeImage = 255 - shapeImage;

		Cv2.Erode (shapeImage, shapeImage, new Mat (), new Point (-1, -1), 5);
		Cv2.Dilate (shapeImage, shapeImage, new Mat (), new Point (-1, -1), 5);

		Mat shapeEdges = new Mat ();
		Cv2.Canny (shapeImage, shapeEdges, 100, 200, 3, false);

		HierarchyIndex[] contoure_hierarcyInd;

		Cv2.FindContours (shapeEdges, out contours, 
							out contoure_hierarcyInd, 
							RetrievalModes.External, 
							ContourApproximationModes.ApproxTC89L1);

		if (contours.Length > 1) {
			Debug.Log ("Dont know witch contour to use.");
			return false;
		}

		generatorState = State.NotSet;

		Point2f center;
		float radius;

		Cv2.MinEnclosingCircle (contours [0], out center, out radius);

		float diameter = radius * 2;

		contourNormalized = new Point2f[contours [0].Length];
		contourScaled = new Point2f[contours [0].Length];

		for (int i = 0; i < contours [0].Length; ++i) {
			contourNormalized [i] = new Point2f ((contours [0] [i].X - center.X) / diameter, 
												(contours [0] [i].Y - center.Y) / diameter);
			contourScaled [i] = new Point2f ();
		}

		steam = new Point2f[2];
		steamNormalized = new Point2f[2];
		steamScaled = new Point2f[2];

		steam [0] = new Point2f(center.X, center.Y);
		steam [0].Y += radius;
		steam [1] = new Point2f(steam [0].X, steam[0].Y);
		steam [1].Y += diameter * maxSteamLengthRatio;

		for (int i = 0; i < steam.Length; ++i) {
			steamNormalized [i] = new Point2f ((steam [i].X - center.X) / diameter, 
												(steam [i].Y - center.Y) / diameter);
			steamScaled [i] = new Point2f ();
		}

		generatorState = State.ShapeSet;

		return true;
	}

	public void SetSize(int size) {
		if (generatorState < State.ShapeSet) {
			Debug.Log ("Shape is not set.");
			return;
		}

		for (int i = 0; i < contours [0].Length; ++i) {
			contourScaled [i].X = contourNormalized [i].X * size;
			contourScaled [i].Y = contourNormalized [i].Y * size;
		}

		for (int i = 0; i < steam.Length; ++i) {
			steamScaled [i].X = steamNormalized [i].X * size;
			steamScaled [i].Y = steamNormalized [i].Y * size;
		}

		generatorState = State.SízeSet;
	}

	public bool SetRotatsionStep(int rotStep) {
		if (generatorState < State.SízeSet) {
			Debug.Log ("Size is not set.");
			return false;
		}

		generatorState = State.SízeSet;

		for (int i = 0; i < contours [0].Length; ++i) {
			contours [0] [i].X = (int)rotatPoint.rotatX (contourScaled [i].X, 
											contourScaled [i].Y, 
											rotStep) 	+ templetCenter;
			contours [0] [i].Y = (int)rotatPoint.rotatY (contourScaled [i].X, 
											contourScaled [i].Y, 
											rotStep) 	+ templetCenter;
		}

		for (int i = 0; i < steam.Length; ++i) {
			steam [i].X = rotatPoint.rotatX (steamScaled [i].X, 
										steamScaled [i].Y, 
										rotStep) 	+ templetCenter;
			steam [i].Y = rotatPoint.rotatY (steamScaled [i].X, 
										steamScaled [i].Y, 
										rotStep) 	+ templetCenter;
		}

		OpenCvSharp.Rect boundBox = Cv2.BoundingRect (contours [0]);

		templetContour = new Mat (templetContureMat, boundBox);
		templetContour.SetTo (new Scalar (0), null);
		Cv2.DrawContours (templetContureMat, contours, 0, new Scalar (255), 1, LineTypes.AntiAlias);

		templetFill = new Mat (templetFillMat, boundBox);
		templetFill.SetTo (new Scalar (0), null);
		// LineTypes.Filled gives error, set thickness to -1 for filling.
		Cv2.DrawContours (templetFillMat, contours, 0, new Scalar (255), -1, LineTypes.Link8);


		matchingArea [0].X = (int)steam [1].X - shiftDistance;
		matchingArea [0].Y = (int)steam [1].Y - shiftDistance;
		matchingArea [1].X = matchingArea [0].X;
		matchingArea [1].Y = (int)steam [1].Y + shiftDistance;
		matchingArea [2].X = (int)steam [1].X + shiftDistance;
		matchingArea [2].Y = matchingArea [1].Y;
		matchingArea [3].X = matchingArea [2].X;
		matchingArea [3].Y = matchingArea [0].Y;

		matchingArea [4].X = boundBox.X - shiftDistance;
		matchingArea [4].Y = boundBox.Y - shiftDistance;
		matchingArea [5].X = matchingArea [4].X;
		matchingArea [5].Y = boundBox.Y + boundBox.Height + shiftDistance;
		matchingArea [6].X = boundBox.X + boundBox.Width + shiftDistance;
		matchingArea [6].Y = matchingArea [5].Y;
		matchingArea [7].X = matchingArea [6].X;
		matchingArea [7].Y = matchingArea [4].Y;

		OpenCvSharp.Rect matchingAreaBounds = Cv2.BoundingRect (matchingArea);

		Point translate = new Point (matchingAreaBounds.X, matchingAreaBounds.Y) - matchingArea [0];

		matchingBoxRect = new OpenCvSharp.Rect(
									uperPlantCenterCorner.X + translate.X, 
									uperPlantCenterCorner.Y + translate.Y,
									matchingAreaBounds.Width,
									matchingAreaBounds.Height);

		// Clamping matching rect to the border of max matching rect.

		if (matchingBoxRect.X < maxMatchingRect.X) {
			int diff = maxMatchingRect.X - matchingBoxRect.X;
			matchingBoxRect.Width -= diff;
			matchingBoxRect.X = maxMatchingRect.X;
		}

		if (matchingBoxRect.Y < maxMatchingRect.Y) {
			int diff = maxMatchingRect.Y - matchingBoxRect.Y;
			matchingBoxRect.Height -= diff;
			matchingBoxRect.Y = maxMatchingRect.Y;
		}

		if (matchingBoxRect.X + matchingBoxRect.Width > maxMatchingRect.X + maxMatchingRect.Width) {
			int diff = matchingBoxRect.X + matchingBoxRect.Width - (maxMatchingRect.X + maxMatchingRect.Width);
			matchingBoxRect.Width -= diff;
		}

		if (matchingBoxRect.Y + matchingBoxRect.Height > maxMatchingRect.Y + maxMatchingRect.Height) {
			int diff = matchingBoxRect.Y + matchingBoxRect.Height - (maxMatchingRect.X + maxMatchingRect.Width);
			matchingBoxRect.Height -= diff;
		}

		// Checke if the matching area is not to small
		if (matchingBoxRect.Width <= templetContour.Width || matchingBoxRect.Height <= templetContour.Height) {
			return false;
		}

		generatorState = State.TempletSet;

		return true;
	}
	
	public OpenCvSharp.Rect GetMatchingRect() {
		if (generatorState < State.TempletSet) {
			Debug.Log ("Templet not set.");
			return new OpenCvSharp.Rect ();
		}
		return matchingBoxRect;
	}

	public Mat GetTemplet() {
		if (generatorState < State.TempletSet) {
			Debug.Log ("Templet not set.");
			return new Mat();
		}
		return templetContour;
	}

	public bool checkAgainstMask(Point location) {
		if (generatorState < State.TempletSet) {
			Debug.Log ("Templet not set.");
			return false;
		}

		double templetValue = Cv2.Sum(templetFill).Val0;

		OpenCvSharp.Rect maskRect = new OpenCvSharp.Rect (matchingBoxRect.X + location.X, 
											matchingBoxRect.Y + location.Y, 
											templetFill.Width, 
											templetFill.Height);

		Mat mask = new Mat (plantMaskInvert, maskRect);
		Mat bitwiseResult = new Mat ();
		Cv2.BitwiseAnd (mask, templetFill, bitwiseResult);

		double outsideMaskValue = Cv2.Sum (bitwiseResult).Val0;
		double templetOutsideMaskRatio = 0.0;
		if (templetValue > 0.0) {
			templetOutsideMaskRatio = outsideMaskValue / templetValue;
		}

		if (templetOutsideMaskRatio > outsideMaskRatio) {
			return false;
		}
		return true;
	}
}

public class RotatPoint {
	struct cosSin {
		public float cos, sin;
	}
	cosSin[] rotStepCosSin;

	public RotatPoint(int numberOfRotationSteps) {
		rotStepCosSin = new cosSin[numberOfRotationSteps];
		float theta;
		float stepToRadianFactor = 360.0f / numberOfRotationSteps * 2 * Mathf.PI / 360;
		for (int i = 0; i < numberOfRotationSteps; ++i) {
			theta = i * stepToRadianFactor;
			rotStepCosSin [i].cos = Mathf.Cos (theta);
			rotStepCosSin [i].sin = Mathf.Sin (theta);
		}
	}

	public float rotatX(float x, float y, int step) {
		return x * rotStepCosSin [step].cos - y * rotStepCosSin [step].sin;
	}

	public float rotatY(float x, float y, int step) {
		return x * rotStepCosSin [step].sin + y * rotStepCosSin [step].cos;
	}
}
