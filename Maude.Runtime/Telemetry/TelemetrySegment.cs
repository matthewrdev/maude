using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ansight.Adb.Telemetry;
using Ansight.Concurrency;
using Ansight.Utilities;

namespace Maude.Runtime.Telemetry
{
    [DebuggerDisplay("{Group}{Id}")]
    public class TelemetrySegment : IMutableTelemetrySegment
    {
        readonly Logging.ILogger log = Logging.Logger.Create();

        public TelemetrySegment(string group,
                                DateTime startUtc,
                                ITelemetryChannel channel)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            Group = group;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Id = Guid.NewGuid();
            this.startUtc.Set(startUtc);
        }

        public TelemetrySegment(string group,
                                List<TelemetryDataPoint> data,
                                DateTime startUtc,
                                DateTime endUtc,
                                double minValue,
                                double maxValue,
                                Guid id,
                                ITelemetryChannel channel,
                                bool isEditable)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (startUtc > endUtc)
            {
                throw new InvalidOperationException($"The {nameof(startUtc)} value must be less than the {nameof(endUtc)} value for this {nameof(TelemetrySegment)}.");
            }

            if (minValue > maxValue)
            {
                throw new InvalidOperationException($"The {nameof(minValue)} value must be less than the {nameof(maxValue)} value for this {nameof(TelemetrySegment)}.");
            }

            Group = group;
            Id = id;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));

            this.data.Set(data);
            this.startUtc.Set(startUtc);
            this.endUtc.Set(endUtc);
            this.minValue.Set(minValue);
            this.maxValue.Set(maxValue);
            this.isEditable.Set(isEditable);
        }

        public string Group { get; }

        public ITelemetryChannel Channel { get; }

        public Guid Id { get; }

        private readonly ConcurrentValue<bool> isEditable = new ConcurrentValue<bool>(true);
        public bool IsEditable => isEditable.Get();

        private readonly ConcurrentValue<List<TelemetryDataPoint>> data = new ConcurrentValue<List<TelemetryDataPoint>>(new List<TelemetryDataPoint>());
        public IReadOnlyList<TelemetryDataPoint> Data => data.Get(value => value.ToList()); // Create a shallow copy.

        public bool IsEmpty => Data.Count > 0;

        private readonly ConcurrentValue<DateTime> startUtc = new ConcurrentValue<DateTime>(DateTime.MinValue);
        public DateTime StartUtc => startUtc.Get();

        private readonly ConcurrentValue<DateTime> endUtc = new ConcurrentValue<DateTime>(DateTime.MinValue);
        public DateTime EndUtc => endUtc.Get();

        private readonly ConcurrentValue<double?> minValue = new ConcurrentValue<double?>(null);
        public double? MinValue => minValue.Get();

        private readonly ConcurrentValue<double?> maxValue = new ConcurrentValue<double?>(null);
        public double? MaxValue => maxValue.Get();

        public event EventHandler<TelemetrySegmentDataChangedEventArgs> OnDataPointsAdded;

        public event EventHandler<TelemetrySegmentDataChangedEventArgs> OnDataPointsRemoved;

        public event EventHandler<TelemetrySegmentMinValueChangedEventArgs> OnMinValueChanged;

        public event EventHandler<TelemetrySegmentMaxValueChangedEventArgs> OnMaxValueChanged;

        public event EventHandler<TelemetrySegmentEndUtcChangedEventArgs> OnEndUtcChanged;

        public event EventHandler<TelemetrySegmentStartUtcChangedEventArgs> OnStartUtcChanged;

        public void AddData(IReadOnlyList<TelemetryDataPoint> dataPoints)
        {
            if (dataPoints is null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            if (dataPoints.Count == 0)
            {
                return;
            }

            var minValue = dataPoints.Min(d => d.Value);
            var maxValue = dataPoints.Max(d => d.Value);
            var endUtc = dataPoints.Max(d => d.DateTimeUtc);

            AddData(dataPoints.OrderBy(d => d.DateTimeUtc).ToList(), minValue, maxValue, endUtc);
        }

        public void AddData(IReadOnlyList<TelemetryDataPoint> dataPoints, double minValue, double maxValue, DateTime endUtc)
        {
            if (dataPoints is null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            if (dataPoints.Count == 0)
            {
                return;
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            this.data.Mutate(d => d.AddRange(dataPoints));
            this.OnDataPointsAdded?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, dataPoints));

            TryChangeMinValue(minValue);
            TryChangeMaxValue(maxValue);
            TryChangeEndUtc(endUtc);
        }
        private void TryChangeStartUtc(DateTime newValue, bool force = false)
        {
            if (newValue < StartUtc
                || force)
            {
                var oldValue = this.StartUtc;
                this.startUtc.Set(newValue);

                OnStartUtcChanged?.Invoke(this, new TelemetrySegmentStartUtcChangedEventArgs(this, oldValue, newValue));
            }
        }


        private void TryChangeEndUtc(DateTime newValue, bool force = false)
        {
            if (newValue > EndUtc
                || force)
            {
                var oldValue = this.EndUtc;
                this.endUtc.Set(newValue);

                OnEndUtcChanged?.Invoke(this, new TelemetrySegmentEndUtcChangedEventArgs(this, oldValue, newValue));
            }
        }

        private void TryChangeMaxValue(double newValue, bool force = false)
        {
            if (!MaxValue.HasValue
                || newValue > MaxValue
                || force)
            {
                var oldValue = this.MaxValue;
                this.maxValue.Set(newValue);

                OnMaxValueChanged?.Invoke(this, new TelemetrySegmentMaxValueChangedEventArgs(this, oldValue, newValue));
            }
        }

        private void TryChangeMinValue(double newValue, bool force = false)
        {
            if (!MinValue.HasValue
                || newValue < MinValue
                || force)
            {
                var oldValue = this.MinValue;
                this.minValue.Set(newValue);

                OnMinValueChanged?.Invoke(this, new TelemetrySegmentMinValueChangedEventArgs(this, oldValue, newValue));
            }
        }

        public void AddData(TelemetryDataPoint dataPoint)
        {
            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            this.data.Mutate(d => d.Add(dataPoint));
            this.OnDataPointsAdded?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, dataPoint.AsList()));

            TryChangeMinValue(dataPoint.Value);
            TryChangeMaxValue(dataPoint.Value);
            TryChangeStartUtc(dataPoint.DateTimeUtc);
            TryChangeEndUtc(dataPoint.DateTimeUtc);
        }

        public void CloseEditing()
        {
            isEditable.Set(false);
        }

        public void RemoveData(IReadOnlyList<TelemetryDataPoint> dataPoints)
        {
            if (dataPoints is null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            throw new NotImplementedException();
        }

        public void Recalculate()
        {
            TelemetrySegmentHelper.TryGetBoundariesForData(Data,
                                                           out var startUtc,
                                                           out var endUtc,
                                                           out var minValue,
                                                           out var maxValue);

            TryChangeMinValue(minValue, force:true);
            TryChangeMaxValue(maxValue, force:true);
            TryChangeStartUtc(startUtc, force:true);
            TryChangeEndUtc(endUtc, force:true);
        }

        public void RemoveData(Func<TelemetryDataPoint, bool> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            if (IsEmpty)
            {
                return;
            }

            IReadOnlyList<TelemetryDataPoint> removedPoints = Array.Empty<TelemetryDataPoint>();
            data.Mutate(d =>
            {
                removedPoints = d.Where(predicate).ToList();
                foreach (var point in removedPoints)
                {
                    d.Remove(point);
                }
            });

            if (removedPoints.Any())
            {
                OnDataPointsRemoved?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, removedPoints));
            }

            Recalculate();
        }

        public void RemoveBefore(DateTime dateTimeUtc)
        {
            if (IsEmpty)
            {
                return;
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            if (dateTimeUtc < this.StartUtc)
            {
                return;
            }

            // Everything is before the given time, clear the collection.
            if (dateTimeUtc > this.EndUtc)
            {
                Clear();
                return;
            }

            var index = TelemetrySegmentHelper.BinarySearchBefore(Data, dateTimeUtc);
            if (index == -1)
            {
                return;
            }

            IReadOnlyList<TelemetryDataPoint> removedPoints = Array.Empty<TelemetryDataPoint>();
            // Remove everything.
            data.Mutate(d =>
            {
                removedPoints = d.GetRange(0, index + 1);
                d.RemoveRange(0, index + 1);
            });

            OnDataPointsRemoved?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, removedPoints));
            Recalculate();
        }

        public void Clear()
        {
            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            IReadOnlyList<TelemetryDataPoint> removedPoints = Array.Empty<TelemetryDataPoint>();
            // Remove everything.
            data.Mutate(d =>
            {
                removedPoints = d.ToList();
                d.Clear();
            });

            OnDataPointsRemoved?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, removedPoints));
            Recalculate();
        }

        public void RemoveAfter(DateTime dateTimeUtc)
        {
            if (IsEmpty)
            {
                return;
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            if (dateTimeUtc > this.EndUtc)
            {
                return;
            }

            if (dateTimeUtc < this.StartUtc)
            {
                Clear();
                return;
            }

            var index = TelemetrySegmentHelper.BinarySearchAfter(Data, dateTimeUtc);
            if (index == -1)
            {
                return;
            }

            IReadOnlyList<TelemetryDataPoint> removedPoints = Array.Empty<TelemetryDataPoint>();
            // Remove everything.
            data.Mutate(d =>
            {
                removedPoints = d.GetRange(index, d.Count - index);
                d.RemoveRange(index, d.Count - index);
            });

            OnDataPointsRemoved?.Invoke(this, new TelemetrySegmentDataChangedEventArgs(this, removedPoints));
            Recalculate();
        }

        public IReadOnlyList<TelemetryDataPoint> GetPointsInRange(DateTime startUtc, DateTime endUtc)
        {
            if (startUtc > this.EndUtc)
            {
                return Array.Empty<TelemetryDataPoint>();
            }

            if (endUtc < this.StartUtc)
            {
                return Array.Empty<TelemetryDataPoint>();
            }

            if (startUtc <= StartUtc && endUtc >= EndUtc)
            {
                return Data;
            }

            return this.data.Get<IReadOnlyList<TelemetryDataPoint>>(value =>
            {
                var startIndex = TelemetrySegmentHelper.BinarySearchAfter(value, startUtc);
                var endIndex = TelemetrySegmentHelper.BinarySearchAfter(value, endUtc);
                if (startIndex == -1 || endIndex == -1)
                {
                    return Array.Empty<TelemetryDataPoint>();
                }

                var count = endIndex - startIndex;
                if (count <= 0)
                {
                    return Array.Empty<TelemetryDataPoint>();
                }

                return value.GetRange(startIndex, count);
            });
        }
    }
}