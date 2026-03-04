using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenCvSharp;

namespace RasterToSpline.Core
{
    /// <summary>
    /// Provides raster-image to vector-contour extraction.
    /// All heavy lifting is delegated to OpenCV via the OpenCvSharp4 wrapper.
    /// The output is intentionally kept as plain <see cref="System.Drawing.PointF"/>
    /// lists so callers (including 3ds Max plug-ins built on .NET Framework 4.8)
    /// do not need an OpenCV dependency themselves.
    /// </summary>
    public static class ContourTracer
    {
        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a raster image, pre-processes it and returns its contours as
        /// lists of pixel-space points.
        /// </summary>
        /// <param name="imagePath">
        ///   Absolute or relative path to a PNG or JPEG file.
        /// </param>
        /// <param name="targetWidthPx">
        ///   The image is down-scaled so its width equals this value before any
        ///   processing.  Aspect ratio is preserved.  Pass the original image
        ///   width (or <c>0</c>) to skip scaling.
        /// </param>
        /// <param name="useAdaptiveThreshold">
        ///   <c>true</c>  → adaptive (Gaussian block-based) threshold.<br/>
        ///   <c>false</c> → global Otsu threshold.
        /// </param>
        /// <param name="canny1">Lower hysteresis threshold for the Canny edge detector.</param>
        /// <param name="canny2">Upper hysteresis threshold for the Canny edge detector.</param>
        /// <param name="minContourArea">
        ///   Contours whose bounding area (in pixels²) is smaller than this
        ///   value are discarded.
        /// </param>
        /// <returns>
        ///   A list of contours.  Each contour is itself a list of
        ///   <see cref="System.Drawing.PointF"/> in image-pixel coordinates of
        ///   the <em>processed</em> (possibly resized) image.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Thrown when <paramref name="imagePath"/> is null or empty.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///   Thrown when the file does not exist on disk.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   Thrown when OpenCV cannot decode the file.
        /// </exception>
        public static List<List<PointF>> TraceContours(
            string imagePath,
            int    targetWidthPx,
            bool   useAdaptiveThreshold,
            int    canny1,
            int    canny2,
            int    minContourArea)
        {
            // ── Validate inputs ───────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentNullException(nameof(imagePath));

            if (!File.Exists(imagePath))
                throw new FileNotFoundException(
                    $"Image not found: {imagePath}", imagePath);

            if (canny1 < 0 || canny2 < 0 || canny1 > canny2)
                throw new ArgumentException(
                    $"Canny thresholds must satisfy 0 ≤ canny1 ≤ canny2. " +
                    $"Got canny1={canny1}, canny2={canny2}.");

            // ── Pipeline ──────────────────────────────────────────────────────
            using Mat original  = LoadImage(imagePath);
            using Mat resized   = Resize(original, targetWidthPx);
            using Mat gray      = ToGrayscale(resized);
            using Mat binary    = Binarize(gray, useAdaptiveThreshold);
            using Mat cleaned   = MorphologicalClean(binary);
            using Mat edges     = DetectEdges(cleaned, canny1, canny2);

            return ExtractContours(edges, minContourArea);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pipeline steps
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the image from disk using OpenCV (supports PNG, JPEG, BMP, TIFF…).
        /// </summary>
        private static Mat LoadImage(string path)
        {
            // ImreadModes.Color keeps a consistent 3-channel BGR layout.
            Mat mat = Cv2.ImRead(path, ImreadModes.Color);

            if (mat.Empty())
                throw new InvalidOperationException(
                    $"OpenCV could not decode the image at '{path}'. " +
                    $"Ensure the file is a valid PNG or JPEG.");

            return mat;
        }

        /// <summary>
        /// Scales the image so its width equals <paramref name="targetWidthPx"/>,
        /// preserving the original aspect ratio.  Returns a new <see cref="Mat"/>.
        /// If <paramref name="targetWidthPx"/> is ≤ 0 or already equals the
        /// source width the original is returned un-touched (cloned so the
        /// caller can dispose it uniformly).
        /// </summary>
        private static Mat Resize(Mat src, int targetWidthPx)
        {
            if (targetWidthPx <= 0 || targetWidthPx == src.Width)
                return src.Clone();

            double scale  = (double)targetWidthPx / src.Width;
            int    newH   = (int)Math.Round(src.Height * scale);

            // Use INTER_AREA for down-scaling (best anti-alias), INTER_LINEAR
            // for up-scaling.
            var interpolation = scale < 1.0
                ? InterpolationFlags.Area
                : InterpolationFlags.Linear;

            Mat dst = new Mat();
            Cv2.Resize(src, dst, new OpenCvSharp.Size(targetWidthPx, newH),
                       interpolation: interpolation);
            return dst;
        }

        /// <summary>
        /// Converts a BGR image to 8-bit single-channel grayscale.
        /// </summary>
        private static Mat ToGrayscale(Mat src)
        {
            Mat gray = new Mat();

            if (src.Channels() == 1)
                return src.Clone();   // already gray

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        /// <summary>
        /// Applies either an adaptive (Gaussian) threshold or a global Otsu
        /// threshold to produce a binary image.
        /// </summary>
        /// <remarks>
        /// Adaptive threshold is better for images with uneven illumination
        /// (e.g. scanned hand-drawings).  Otsu is faster and works well for
        /// high-contrast images like technical diagrams.
        /// </remarks>
        private static Mat Binarize(Mat gray, bool useAdaptive)
        {
            Mat binary = new Mat();

            if (useAdaptive)
            {
                // Block size must be odd and > 1.  11 is a sensible default
                // that handles moderate lighting variation.
                const int blockSize = 11;
                const double c      = 2.0;  // constant subtracted from mean

                Cv2.AdaptiveThreshold(
                    gray, binary,
                    maxValue:         255,
                    adaptiveMethod:   AdaptiveThresholdTypes.GaussianC,
                    thresholdType:    ThresholdTypes.BinaryInv,
                    blockSize:        blockSize,
                    c:                c);
            }
            else
            {
                // ThresholdTypes.Otsu automatically selects the optimal
                // global threshold; the supplied threshold value (0) is ignored.
                Cv2.Threshold(
                    gray, binary,
                    thresh:    0,
                    maxval:    255,
                    type:      ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
            }

            return binary;
        }

        /// <summary>
        /// Applies morphological close then open operations to remove small
        /// noise pixels and fill minor gaps in strokes.
        /// </summary>
        private static Mat MorphologicalClean(Mat binary)
        {
            // A 3×3 elliptical kernel is a good general-purpose choice:
            // it avoids the "blockiness" of a rectangular kernel while still
            // being very fast.
            using Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(3, 3));

            Mat closed = new Mat();
            Mat opened = new Mat();

            // Close: dilate → erode  (fills small holes / gaps)
            Cv2.MorphologyEx(binary, closed, MorphTypes.Close, kernel,
                             iterations: 2);

            // Open:  erode  → dilate (removes isolated noise pixels)
            Cv2.MorphologyEx(closed, opened, MorphTypes.Open, kernel,
                             iterations: 1);

            closed.Dispose();
            return opened;
        }

        /// <summary>
        /// Runs the Canny edge detector on the cleaned binary image.
        /// </summary>
        private static Mat DetectEdges(Mat cleaned, int canny1, int canny2)
        {
            Mat edges = new Mat();

            // apertureSize=3 (Sobel kernel) is the standard default.
            // L2gradient=true gives slightly more accurate gradient magnitude
            // at a negligible performance cost.
            Cv2.Canny(cleaned, edges,
                      threshold1:    canny1,
                      threshold2:    canny2,
                      apertureSize:  3,
                      L2gradient:    true);

            return edges;
        }

        /// <summary>
        /// Finds external contours in the edge image, filters by minimum area,
        /// and converts OpenCV point arrays to
        /// <see cref="System.Drawing.PointF"/> lists.
        /// </summary>
        private static List<List<PointF>> ExtractContours(Mat edges, int minArea)
        {
            // RETR_EXTERNAL: only the outermost contour of each connected
            //   region — sufficient for spline generation and avoids duplicate
            //   inner paths.
            // CHAIN_APPROX_TC89_KCOS: Teh-Chin chain approximation reduces the
            //   number of points while faithfully preserving the curve shape,
            //   which is ideal for spline control-point density.
            Cv2.FindContours(
                edges,
                out OpenCvSharp.Point[][] rawContours,
                out _,  // hierarchy — not needed for RETR_EXTERNAL
                RetrievalModes.External,
                ContourApproximationModes.ApproxTC89KCOS);

            var result = new List<List<PointF>>(rawContours.Length);

            foreach (OpenCvSharp.Point[] contour in rawContours)
            {
                // Fast bounding-rect area check — cheaper than contourArea
                // and good enough for noise filtering.
                double area = Cv2.ContourArea(contour);
                if (area < minArea)
                    continue;

                var points = new List<PointF>(contour.Length);
                foreach (OpenCvSharp.Point p in contour)
                    points.Add(new PointF(p.X, p.Y));

                result.Add(points);
            }

            return result;
        }
    }
}
