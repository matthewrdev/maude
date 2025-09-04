using System;
using System.Collections.Generic;
using System.Linq;
using Ansight.Adb.Telemetry;
using Ansight.Concurrency;

namespace Maude.Runtime.Telemetry
{
    public class TelemetryChannel : IMutableTelemetryChannel
    {
        readonly Logging.ILogger log = Logging.Logger.Create();

        public TelemetryChannel(string name,
                                ITelemetrySink sink,
                                bool isEditable)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.isEditable.Set(isEditable);
        }

        public TelemetryChannel(string name,
                                ITelemetrySink sink,
                                IReadOnlyList<IMutableTelemetrySegment> segments)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }


            if (segments is null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            Name = name;
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.segments.Mutate(s => s.AddRange(segments));
            this.endUtc.Set(segments.Max(s => s.EndUtc));
            this.isEditable.Set(false);
        }

        private readonly ConcurrentValue<bool> isEditable = new ConcurrentValue<bool>(true);
        public bool IsEditable => isEditable.Get();

        public string Name { get; }

        public ITelemetrySink Sink { get; }

        public IReadOnlyList<string> Groups => Segments.Select(s => s.Group).Distinct().ToList();

        private readonly ConcurrentValue<List<IMutableTelemetrySegment>> segments = new ConcurrentValue<List<IMutableTelemetrySegment>>(new List<IMutableTelemetrySegment>());
        public IReadOnlyList<ITelemetrySegment> Segments => segments.Get(s => s.ToList()); // Create a shallow copy.

        public DateTime StartUtc => segments.Get(values => values.Min(s => s.StartUtc));

        private readonly ConcurrentValue<DateTime> endUtc = new ConcurrentValue<DateTime>(DateTime.MinValue);
        public DateTime EndUtc => endUtc.Get();

        public event EventHandler<TelemetrySegmentsChangedEventArgs> OnTelemetrySegmentsOpened;

        public event EventHandler<TelemetrySegmentsChangedEventArgs> OnTelemetrySegmentsClosed;
        public event EventHandler<TelemetryChannelEndUtcChangedEventArgs> OnEndUtcChanged;

        public void CloseEditing()
        {
            isEditable.Set(false);

            this.segments.Mutate(segments =>
            {
                foreach (var segment in segments)
                {
                    if (segment.IsEditable)
                    {
                        segment.CloseEditing();
                    }
                }
            });
        }

        public IMutableTelemetrySegment GetCurrentSegment(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            var segment = this.segments.Get(values =>
            {
                return values.Where(v => v.Group == group)
                             .OrderBy(v => v.EndUtc)
                             .LastOrDefault();
            });

            return segment;
        }

        public IMutableTelemetrySegment OpenSegmentForGroup(string group, DateTime startUtc)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            CloseSegmentForGroup(group);

            var segment = new TelemetrySegment(group, startUtc, this);
            segment.OnEndUtcChanged += Segment_OnEndUtcChanged;
            this.segments.Mutate(value => value.Add(segment));

            try
            {
                OnTelemetrySegmentsOpened?.Invoke(this, new TelemetrySegmentsChangedEventArgs(segment));
            }
            catch (Exception ex)
            {
                log?.Exception(ex);
            }

            return segment;
        }

        public void CloseSegmentForGroups(IReadOnlyList<string> groups)
        {
            if (groups is null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group))
                {
                    continue;
                }

                CloseSegmentForGroup(group);
            }
        }

        public void CloseSegmentForGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            var currentSegment = GetCurrentSegment(group);
            if (currentSegment != null && currentSegment.IsEditable)
            {
                currentSegment.CloseEditing();
                currentSegment.OnEndUtcChanged -= Segment_OnEndUtcChanged;

                try
                {
                    OnTelemetrySegmentsClosed?.Invoke(this, new TelemetrySegmentsChangedEventArgs(currentSegment));
                }
                catch (Exception ex)
                {
                    log?.Exception(ex);
                }
            }
        }

        public IReadOnlyList<ITelemetrySegment> GetSegmentsForGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            return this.segments.Get(segmentsValue =>
            {
                return segmentsValue.Where(segment => segment.Group == group).ToList();
            });
        }

        public IReadOnlyList<ITelemetrySegment> GetSegmentsInRange(DateTime startUtc, DateTime endUtc)
        {
            return this.segments.Get(data =>
            {
                var result = new List<ITelemetrySegment>();

                foreach (var segment in data)
                {
                    if (segment.EndUtc < startUtc)
                    {
                        continue;
                    }

                    if (segment.StartUtc > endUtc)
                    {
                        continue;
                    }

                    result.Add(segment);
                }

                return result;
            });
        }

        public IReadOnlyList<ITelemetrySegment> GetSegmentsInRangeForGroup(string groupName, DateTime startUtc, DateTime endUtc)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"'{nameof(groupName)}' cannot be null or empty.", nameof(groupName));
            }


            return this.segments.Get(data =>
            {
                var result = new List<ITelemetrySegment>();

                foreach (var segment in data)
                {
                    if (segment.Group != groupName)
                    {
                        continue;
                    }

                    if (segment.EndUtc < startUtc)
                    {
                        continue;
                    }

                    if (segment.StartUtc > endUtc)
                    {
                        continue;
                    }

                    result.Add(segment);
                }

                return result;
            });
        }

        private void Segment_OnEndUtcChanged(object sender, TelemetrySegmentEndUtcChangedEventArgs e)
        {
            if (e.NewEndUtc > this.EndUtc)
            {
                var old = this.EndUtc;
                endUtc.Set(e.NewEndUtc);

                this.OnEndUtcChanged?.Invoke(this, new TelemetryChannelEndUtcChangedEventArgs(this, old, e.NewEndUtc));
            }
            else
            {
            }
        }
    }
}