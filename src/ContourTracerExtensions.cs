using System;
using System.Collections.Generic;
using System.Drawing;

namespace RasterToSpline.Core
{
    /// <summary>
    /// Convenience overloads for <see cref="ContourTracer"/>.
    /// </summary>
    public static class ContourTracerExtensions
    {
        /// <summary>
        /// Traces contours using a <see cref="ContourTracerOptions"/> parameter
        /// bag instead of individual parameters.
        /// </summary>
        /// <param name="options">Pre-configured options object.</param>
        /// <returns>
        /// A list of contours, each expressed as a list of pixel-space
        /// <see cref="PointF"/> values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public static List<List<PointF>> TraceContours(
            this ContourTracerOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            return ContourTracer.TraceContours(
                imagePath:            options.ImagePath,
                targetWidthPx:        options.TargetWidthPx,
                useAdaptiveThreshold: options.UseAdaptiveThreshold,
                canny1:               options.Canny1,
                canny2:               options.Canny2,
                minContourArea:       options.MinContourArea);
        }
    }
}
