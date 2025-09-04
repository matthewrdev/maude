using System;
using Ansight.Adb.Logcat;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public static class TelemetrySegmentHelper
    {
        /// <summary>
        /// When the <see cref="BinarySearch(IReadOnlyList{TelemetryDataPoint}, Func{TelemetryDataPoint, bool}, int, int)"/> cannot satisfy the provided predicate however the left and right indices are next to each other, which one should they search select?
        /// </summary>
        public enum BinarySeachFallbackSelectionBehaviour
        {
            LeftIndex,

            RightIndex,
        }

        /// <summary>
        /// For the given <paramref name="startUtc"/>, finds the boundaries of the dataset.
        /// </summary>
        /// <param name="telemetryDataPoints"></param>
        /// <param name="startUtc"></param>
        /// <param name="endUtc"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool TryGetBoundariesForData(IReadOnlyList<TelemetryDataPoint> telemetryDataPoints,
                                                   out DateTime startUtc,
                                                   out DateTime endUtc,
                                                   out double minValue,
                                                   out double maxValue)
        {
            if (telemetryDataPoints is null)
            {
                throw new ArgumentNullException(nameof(telemetryDataPoints));
            }

            startUtc = DateTime.MinValue;
            endUtc = DateTime.MaxValue;
            minValue = double.NaN;
            maxValue = double.NaN;

            if (telemetryDataPoints.Count == 0)
            {
                return false;
            }

            bool isFirst = true;
            foreach (var point in telemetryDataPoints)
            {
                if (isFirst)
                {
                    isFirst = false;
                    startUtc = point.DateTimeUtc;
                    endUtc = point.DateTimeUtc;
                    minValue = point.Value;
                    maxValue = point.Value;
                }
                else
                {
                    if (point.DateTimeUtc < startUtc)
                    {
                        startUtc = point.DateTimeUtc;
                    }

                    if (point.DateTimeUtc > endUtc)
                    {
                        endUtc = point.DateTimeUtc;
                    }

                    if (point.Value < minValue)
                    {
                        minValue = point.Value;
                    }

                    if (point.Value > maxValue)
                    {
                        maxValue = point.Value;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Performs a binary search on <paramref name="telemetryPointData"/> to efficiently find the first <see cref="TelemetryDataPoint"/> that is before the <paramref name="dateTime"/>.
        /// <para/>
        /// If the <paramref name="capturedAtUtc"/> is not within the range of the <paramref name="telemetryPointData"/>, returns -1.
        /// <para/>
        /// The provided <paramref name="telemetryPointData"/> must be ordered by <see cref="TelemetryDataPoint.DateTimeUtc"/> in ascending order.
        /// </summary>
        public static int BinarySearchBefore(IReadOnlyList<TelemetryDataPoint> telemetryPointData,
                                             DateTime dateTime)
        {
            if (telemetryPointData is null)
            {
                throw new ArgumentNullException(nameof(telemetryPointData));
            }

            bool predicate(TelemetryDataPoint dataPoint)
            {
                return dataPoint.DateTimeUtc < dateTime;
            }

            return BinarySearch(telemetryPointData, predicate, BinarySeachFallbackSelectionBehaviour.LeftIndex);
        }

        /// <summary>
        /// Performs a binary search on <paramref name="telemetryPointData"/> to efficiently find the first <see cref="TelemetryDataPoint"/> that is after the <paramref name="dateTime"/>.
        /// <para/>
        /// If the <paramref name="capturedAtUtc"/> is not within the range of the <paramref name="telemetryPointData"/>, returns -1.
        /// <para/>
        /// The provided <paramref name="telemetryPointData"/> must be ordered by <see cref="TelemetryDataPoint.DateTimeUtc"/> in ascending order.
        /// </summary>
        public static int BinarySearchAfter(IReadOnlyList<TelemetryDataPoint> telemetryPointData,
                                             DateTime dateTime)
        {
            if (telemetryPointData is null)
            {
                throw new ArgumentNullException(nameof(telemetryPointData));
            }

            bool predicate(TelemetryDataPoint dataPoint)
            {
                return dataPoint.DateTimeUtc > dateTime;
            }

            return BinarySearch(telemetryPointData, predicate, BinarySeachFallbackSelectionBehaviour.RightIndex);
        }

        /// <summary>
        /// Performs a binary search on <paramref name="telemetryPointData"/> to efficiently find the closest <see cref="ILogEntry"/> to the <paramref name="capturedAtUtc"/>.
        /// <para/>
        /// If the <paramref name="capturedAtUtc"/> is not within the range of the <paramref name="telemetryPointData"/>, returns -1.
        /// <para/>
        /// The provided <paramref name="telemetryPointData"/> must be ordered by <see cref="TelemetryDataPoint.DateTimeUtc"/> in ascending order.
        /// </summary>
        public static int BinarySearch(IReadOnlyList<TelemetryDataPoint> telemetryPointData,
                                       Func<TelemetryDataPoint, bool> predicate,
                                       BinarySeachFallbackSelectionBehaviour selectionBehaviour)
        {
            if (telemetryPointData is null)
            {
                throw new ArgumentNullException(nameof(telemetryPointData));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (telemetryPointData.Count == 0)
            {
                return -1;
            }

            if (telemetryPointData.Count == 1)
            {
                return 0;
            }

            if (telemetryPointData[1].DateTimeUtc < telemetryPointData[0].DateTimeUtc)
            {
                throw new ArgumentException("The provided telemetry data must be sorted by DateTimeUtc in ascending order", nameof(telemetryPointData));
            }

            return BinarySearch(telemetryPointData, predicate, 0, telemetryPointData.Count - 1, selectionBehaviour);
        }

        /// <summary>
        /// Applies a recursive binary search on the <paramref name="telemetryPointData"/> to find the <see cref="TelemetryDataPoint"/> that satisfies the <paramref name="predicate"/> conditions within the <paramref name="leftIndex"/> and <paramref name="rightIndex"/>.
        /// </summary>
        private static int BinarySearch(IReadOnlyList<TelemetryDataPoint> telemetryPointData,
                                        Func<TelemetryDataPoint, bool> predicate,
                                        int leftIndex,
                                        int rightIndex,
                                        BinarySeachFallbackSelectionBehaviour selectionBehaviour)
        {
            if (telemetryPointData is null)
            {
                throw new ArgumentNullException(nameof(telemetryPointData));
            }

            if (leftIndex == rightIndex)
            {
                return leftIndex;
            }

            if (leftIndex + 1 == rightIndex)
            {
                return selectionBehaviour == BinarySeachFallbackSelectionBehaviour.LeftIndex ? leftIndex : rightIndex; 
            }

            var midPoint = (int)Math.Floor((rightIndex - leftIndex) / 2.0d) + leftIndex;

            var entry = telemetryPointData[midPoint];
            if (predicate(entry))
            {
                return BinarySearch(telemetryPointData, predicate, leftIndex, midPoint, selectionBehaviour);
            }

            return BinarySearch(telemetryPointData, predicate, midPoint, rightIndex, selectionBehaviour);
        }
    }
}

