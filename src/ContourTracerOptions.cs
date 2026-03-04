namespace RasterToSpline.Core
{
    /// <summary>
    /// Convenience parameter bag that groups all <see cref="ContourTracer"/>
    /// settings with sensible defaults.  Callers can construct one, tweak the
    /// properties they care about, and pass them straight into
    /// <see cref="ContourTracer.TraceContours(ContourTracerOptions)"/>.
    /// </summary>
    public sealed class ContourTracerOptions
    {
        // ── Input ──────────────────────────────────────────────────────────

        /// <summary>
        /// Absolute or relative path to the source PNG or JPEG image.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        // ── Pre-processing ─────────────────────────────────────────────────

        /// <summary>
        /// Width in pixels to which the image is scaled before processing.
        /// The aspect ratio is always preserved.
        /// Set to <c>0</c> to skip resizing.
        /// Default: <c>1024</c>.
        /// </summary>
        public int TargetWidthPx { get; set; } = 1024;

        /// <summary>
        /// <c>true</c>  → adaptive (Gaussian block-based) thresholding, good
        ///               for uneven lighting (scanned drawings, photos).<br/>
        /// <c>false</c> → global Otsu auto-threshold, ideal for high-contrast
        ///               technical diagrams.
        /// Default: <c>false</c> (Otsu).
        /// </summary>
        public bool UseAdaptiveThreshold { get; set; } = false;

        // ── Canny ──────────────────────────────────────────────────────────

        /// <summary>
        /// Lower hysteresis threshold for the Canny edge detector.
        /// Edges with gradient below this value are discarded.
        /// Default: <c>50</c>.
        /// </summary>
        public int Canny1 { get; set; } = 50;

        /// <summary>
        /// Upper hysteresis threshold for the Canny edge detector.
        /// Edges with gradient above this value are always kept.
        /// Default: <c>150</c>.
        /// </summary>
        public int Canny2 { get; set; } = 150;

        // ── Filtering ──────────────────────────────────────────────────────

        /// <summary>
        /// Contours whose pixel area is smaller than this value are discarded.
        /// Raise the value to suppress small noise contours; lower it to
        /// capture fine detail.
        /// Default: <c>100</c> px².
        /// </summary>
        public int MinContourArea { get; set; } = 100;
    }
}
