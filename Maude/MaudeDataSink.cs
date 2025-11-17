using System.Runtime.InteropServices;

namespace Maude;

internal class MaudeMutableDataSink : IMaudeDataSink
{
    private static readonly Comparer<MaudeEvent> EventCapturedAtComparer =
        Comparer<MaudeEvent>.Create((a, b) => a.CapturedAtUtc.CompareTo(b.CapturedAtUtc));
    
    private static readonly Comparer<MaudeMetric> MetricCapturedAtComparer =
        Comparer<MaudeMetric>.Create((a, b) => a.CapturedAtUtc.CompareTo(b.CapturedAtUtc));
    
    private readonly MaudeOptions options;

    public MaudeMutableDataSink(MaudeOptions  options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        this.options = options;

        options.Validate();

        channels[MaudeConstants.ReservedChannels.ClrMemoryUsage_Id] = new MaudeChannel(MaudeConstants.ReservedChannels.ClrMemoryUsage_Id, MaudeConstants.ReservedChannels.ClrMemoryUsage_Name, MaudeConstants.ReservedChannels.ClrMemoryUsage_Color);
        
#if IOS
        channels[MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id] = new MaudeChannel(MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id, MaudeConstants.ReservedChannels.PlatformMemoryUsage_Name, MaudeConstants.ReservedChannels.PlatformMemoryUsage_Color);
#elif ANDROID
        channels[MaudeConstants.ReservedChannels.NativeHeapAllocated_Id] = new MaudeChannel(MaudeConstants.ReservedChannels.NativeHeapAllocated_Id, MaudeConstants.ReservedChannels.NativeHeapAllocated_Name, MaudeConstants.ReservedChannels.NativeHeapAllocated_Color);
        channels[MaudeConstants.ReservedChannels.Rss_Id] = new MaudeChannel(MaudeConstants.ReservedChannels.Rss_Id, MaudeConstants.ReservedChannels.Rss_Name, MaudeConstants.ReservedChannels.Rss_Color);
#endif
        
        channels[MaudeConstants.ReservedChannels.ChannelNotSpecified_Id] = new MaudeChannel(MaudeConstants.ReservedChannels.ChannelNotSpecified_Id, "Not Specified", default(Color));

        if (options.AdditionalChannels != null 
            && options.AdditionalChannels.Count > 0)
        {
            foreach (var channel in options.AdditionalChannels)
            {
                channels[channel.Id] = channel;
            }
        }

        foreach (var channel in channels)
        {
            metricsByChannel[channel.Key] = new List<MaudeMetric>(options.MaximumBufferSize);
            eventsByChannel[channel.Key] = new List<MaudeEvent>(options.MaximumBufferSize);
        }
    }
    
    
    private readonly Lock channelLock = new Lock();
    private readonly Dictionary<byte, MaudeChannel> channels = new Dictionary<byte, MaudeChannel>();
    
    private readonly Lock metricsLock = new Lock();
    private DateTime minMetricsDateTime = DateTime.MaxValue;
    private DateTime maxMetricsDateTime = DateTime.MinValue;
    private readonly Dictionary<byte, List<MaudeMetric>> metricsByChannel = new Dictionary<byte, List<MaudeMetric>>();
    
    private readonly Lock eventsLock = new Lock();
    private DateTime minEventsDateTime = DateTime.MaxValue;
    private DateTime maxEventsDateTime = DateTime.MinValue;
    private readonly Dictionary<byte, List<MaudeEvent>> eventsByChannel = new Dictionary<byte, List<MaudeEvent>>();

    public event EventHandler<MaudeMetricsUpdatedEventArgs>? OnMetricsUpdated;
    public event EventHandler<MaudeEventsUpdatedEventArgs>? OnEventsUpdated;
    
    public IReadOnlyList<MaudeChannel> Channels
    {
        get
        {
            lock (channelLock)
            {
                // Intentional 'ToList' to not pass around a reference to a thread safe value.
                return channels.Values.ToList();
            }
        }
    }
    
    public IReadOnlyList<MaudeMetric> Metrics
    {
        get
        {
            lock (metricsLock)
            {
                var metricsCount = metricsByChannel.Sum(m => m.Value.Count);
                var metrics = new List<MaudeMetric>(metricsCount);

                foreach (var channelValues in metricsByChannel.Values)
                {
                    metrics.AddRange(channelValues);
                }
                
                metrics.Sort();

                return metrics;
            }
        }
    }
    
    public IReadOnlyList<MaudeEvent> Events
    {
        get
        {
            lock (eventsLock)
            {
                var eventsCount = eventsByChannel.Sum(m => m.Value.Count);
                var events = new List<MaudeEvent>(eventsCount);

                foreach (var channelValues in eventsByChannel.Values)
                {
                    events.AddRange(channelValues);
                }

                return events;
            }
        }
    }
    
    public IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return GetMetricsForChannel(channel.Id);
    }

    public IReadOnlyList<MaudeMetric> GetMetricsForChannel(byte channelId)
    {
        lock (metricsLock)
        {
            if (!this.metricsByChannel.TryGetValue(channelId, out var channelData))
            {
                return  Array.Empty<MaudeMetric>();
            }

            // Intentional 'ToList' to not pass around a reference to a thread safe value.
            return channelData.ToList();
        }
    }

    public IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return GetMetricsForChannelInRange(channel.Id, fromUtc, toUtc);
    }

    public IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            fromUtc = toUtc;
        }
        
        lock (metricsLock)
        {
            if (!this.metricsByChannel.TryGetValue(channelId, out var channelData))
            {
                return  Array.Empty<MaudeMetric>();
            }
            
            var isBeforeBounds = toUtc < this.minMetricsDateTime;
            var isAfterBounds = fromUtc > this.maxMetricsDateTime;

            if (isBeforeBounds || isAfterBounds)
            {
                return Array.Empty<MaudeMetric>();
            }

            if (fromUtc < this.minMetricsDateTime
                && toUtc > this.maxMetricsDateTime)
            {
                // Intentional by-reference copy of inner values
                return channelData.ToList();
            }
            
            var size = CalculateBufferSize(fromUtc, toUtc);

            List<MaudeMetric> metrics = new List<MaudeMetric>(size);


            foreach (var metric in channelData)
            {
                if (metric.CapturedAtUtc >= fromUtc && metric.CapturedAtUtc <= toUtc)
                {
                    metrics.Add(metric);
                }

                if (metric.CapturedAtUtc > toUtc)
                {
                    break;
                }
            }
            
            return metrics;
        }
    }

    public MaudeChannelSpan GetMetricsChannelSpanForRange(byte channelId, DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            fromUtc = toUtc;
        }
        
        lock (metricsLock)
        {
            if (!this.metricsByChannel.TryGetValue(channelId, out var channelData))
            {
                return MaudeChannelSpan.Invalid;
            }
            
            var isBeforeBounds = toUtc < this.minMetricsDateTime;
            var isAfterBounds = fromUtc > this.maxMetricsDateTime;

            if (isBeforeBounds || isAfterBounds)
            {
                return MaudeChannelSpan.Invalid;
            }

            var count = 0;
            var minValue = long.MaxValue;
            var maxValue = long.MinValue;

            foreach (var metric in channelData)
            {
                if (metric.CapturedAtUtc >= fromUtc 
                    && metric.CapturedAtUtc <= toUtc)
                {
                    count++;
                    if (metric.Value < minValue)
                    {
                        minValue = metric.Value;
                    }

                    if (metric.Value > maxValue)
                    {
                        maxValue = metric.Value;
                    }
                }

                if (metric.CapturedAtUtc > toUtc)
                {
                    break;
                }
            }
            
            return new MaudeChannelSpan()
            {
                ChannelId = channelId,
                MinValue = minValue,
                MaxValue = maxValue,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Count = count,
                Valid = true
            };
        }
    }

    public void UseMetricsInChannelForRange(byte channelId, DateTime fromUtc, DateTime toUtc, Action<ReadOnlySpan<MaudeMetric>> useAction)
    {
        if (useAction == null) throw new ArgumentNullException(nameof(useAction));

        if (fromUtc > toUtc)
        {
            fromUtc = toUtc;
        }

        lock (metricsLock)
        {
            if (!this.metricsByChannel.TryGetValue(channelId, out var channelData))
            {
                // Invalid or unknown channel, discard request.
                return;
            }

            if (channelData.Count == 0)
            {
                // no channel content, do nothing.
                return;
            }

            if (toUtc < this.minMetricsDateTime
                || fromUtc > this.maxMetricsDateTime)
            {
                // Invalid span range, discard request.
                return;
            }

            var start = 0;
            var end = channelData.Count - 1;

            if (fromUtc > minMetricsDateTime)
            {
                start = BinarySearchHelper.FindFirstIndex(channelData, fromUtc);
            }
            
            if (toUtc < maxMetricsDateTime)
            {
                end = BinarySearchHelper.FindLastIndex(channelData, toUtc);
            }

            var length = Math.Max(0, end - start + 1);
            if (length <= 0)
            {
                return;
            }

            ReadOnlySpan<MaudeMetric> range = CollectionsMarshal.AsSpan(channelData)
                .Slice(start, length);
            
            useAction(range);
        }
    }

    private int CalculateBufferSize(DateTime fromUtc, DateTime toUtc)
    {
        var elapsed = toUtc - fromUtc;
        var estimatedCount = (int)(elapsed.TotalSeconds * (int)Math.Ceiling(1000f / options.SampleFrequencyMilliseconds));

        if (estimatedCount > options.MaximumBufferSize)
        {
            return options.MaximumBufferSize;
        }

        return estimatedCount;
    }

    public IReadOnlyList<MaudeEvent> GetEventsForChannel(MaudeChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return GetEventsForChannel(channel.Id);
    }

    public IReadOnlyList<MaudeEvent> GetEventsForChannel(byte channelId)
    {
        lock (eventsLock)
        {
            if (!this.eventsByChannel.TryGetValue(channelId, out var channelData))
            {
                return  Array.Empty<MaudeEvent>();
            }

            // Intentional 'ToList' to not pass around a reference to a thread safe value.
            return channelData.ToList();
        }
    }

    public IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return GetEventsForChannelInRange(channel.Id, fromUtc, toUtc);
    }

    public IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            fromUtc = toUtc;
        }
        
        lock (eventsLock)
        {
            if (!this.eventsByChannel.TryGetValue(channelId, out var channelData))
            {
                return  Array.Empty<MaudeEvent>();
            }
            
            var isBeforeBounds = toUtc < this.minMetricsDateTime;
            var isAfterBounds = fromUtc > this.maxMetricsDateTime;

            if (isBeforeBounds || isAfterBounds)
            {
                return Array.Empty<MaudeEvent>();
            }

            if (fromUtc < this.minMetricsDateTime
                && toUtc > this.maxMetricsDateTime)
            {
                // Intentional 'ToList' to not pass around a reference to a thread safe value.
                return channelData.ToList();
            }
            
            var size = CalculateBufferSize(fromUtc, toUtc);

            var values = new List<MaudeEvent>(size);

            foreach (var datum in channelData)
            {
                if (datum.CapturedAtUtc >= fromUtc && datum.CapturedAtUtc <= toUtc)
                {
                    values.Add(datum);
                }

                if (datum.CapturedAtUtc > toUtc)
                {
                    break;
                }
            }
            
            return values;
        }
    }

    public void UseEventsInChannelForRange(byte channelId, 
                                           DateTime fromUtc,
                                           DateTime toUtc,
                                           Action<ReadOnlySpan<MaudeEvent>> useAction)
    {
        if (useAction == null) throw new ArgumentNullException(nameof(useAction));

        if (fromUtc > toUtc)
        {
            fromUtc = toUtc;
        }

        lock (eventsLock)
        {
            if (!this.eventsByChannel.TryGetValue(channelId, out var channelData))
            {
                // Invalid or unknown channel, discard request.
                return;
            }

            if (channelData.Count == 0)
            {
                // no channel content, do nothing.
                return;
            }

            if (toUtc < this.minEventsDateTime
                || fromUtc > this.maxEventsDateTime)
            {
                // Invalid span range, discard request.
                return;
            }

            var start = 0;
            var end = channelData.Count - 1;

            if (fromUtc > minEventsDateTime)
            {
                start = BinarySearchHelper.FindFirstIndex(channelData, fromUtc);
            }
            
            if (toUtc < maxEventsDateTime)
            {
                end = BinarySearchHelper.FindLastIndex(channelData, toUtc);
            }
            
            var length = Math.Max(0, end - start + 1);
            if (length == 0)
            {
                return;
            }

            ReadOnlySpan<MaudeEvent> range = CollectionsMarshal.AsSpan(channelData)
                                                                  .Slice(start, length);
            
            useAction(range);
        }
    }

    public void RecordMemorySnapshot(MemorySnapshot snapshot)
    {
        var clr = snapshot.ManagedHeapBytes;
        MutateMetrics(metrics =>
        {
            List<MaudeMetric> added = new List<MaudeMetric>();
            if (metrics.TryGetValue(MaudeConstants.ReservedChannels.ClrMemoryUsage_Id, out var clrMetrics))
            {
                var metric = new MaudeMetric()
                {
                    CapturedAtUtc = DateTime.UtcNow,
                    Channel = MaudeConstants.ReservedChannels.ClrMemoryUsage_Id,
                    Value = clr
                };
                
                added.Add(metric);
                clrMetrics.Add(metric);
            }
            
#if IOS
            var platform = snapshot.RssBytes;
            if (metrics.TryGetValue(MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id, 
                                    out var platformMetrics))
            {
                var metric = new MaudeMetric()
                {
                    CapturedAtUtc = DateTime.UtcNow,
                    Channel = MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id,
                    Value = platform
                };
                
                added.Add(metric);
                platformMetrics.Add(metric);
            }
#elif ANDROID
            AddMetricIfPresent(metrics, added, MaudeConstants.ReservedChannels.NativeHeapAllocated_Id, snapshot.NativeHeapAllocatedBytes);
            AddMetricIfPresent(metrics, added, MaudeConstants.ReservedChannels.Rss_Id, snapshot.RssBytes);
#endif

            return added;
        });
    }

#if ANDROID
    private static void AddMetricIfPresent(Dictionary<byte, List<MaudeMetric>> metrics, List<MaudeMetric> added, byte channelId, long value)
    {
        if (!metrics.TryGetValue(channelId, out var channelMetrics))
        {
            return;
        }

        var metric = new MaudeMetric()
        {
            CapturedAtUtc = DateTime.UtcNow,
            Channel = channelId,
            Value = value
        };

        added.Add(metric);
        channelMetrics.Add(metric);
    }
#endif
    
    public void Metric(long value, byte channel)
    {
        MutateMetrics(metrics =>
        {
            if (!metrics.TryGetValue(channel, out var channelMetrics))
            {
                return Array.Empty<MaudeMetric>();
            }

            var metric = new MaudeMetric()
            {
                CapturedAtUtc = DateTime.UtcNow,
                Channel = channel,
                Value = value
            };
            
            channelMetrics.Add(metric);
            return new List<MaudeMetric>()
            {
                metric
            };
        });
    }

    private void MutateMetrics(Func<Dictionary<byte, List<MaudeMetric>>, IReadOnlyList<MaudeMetric>> mutator)
    {
        if (mutator == null) throw new ArgumentNullException(nameof(mutator));

        IReadOnlyList<MaudeMetric> newValues = null;
        List<MaudeMetric>? trimmedValues = null;
        
        var expiryTime = DateTime.UtcNow - TimeSpan.FromSeconds(options.RetentionPeriodSeconds);
        
        lock (metricsLock)
        {
            newValues = mutator(metricsByChannel);

            foreach (var channel in metricsByChannel.Values)
            {
                var removeCount = 0;
                var initialCount = channel.Count;

                while (removeCount < initialCount && channel[removeCount].CapturedAtUtc < expiryTime)
                {
                    removeCount++;
                }

                if (removeCount > 0)
                {
                    trimmedValues ??= new List<MaudeMetric>();
                    trimmedValues.AddRange(channel.GetRange(0, removeCount));
                    channel.RemoveRange(0, removeCount);
                }
                
                if (channel.Count > 0)
                {
                    var last = channel[^1];
                    var first = channel[0];

                    if (last.CapturedAtUtc > maxMetricsDateTime)
                    {
                        maxMetricsDateTime = last.CapturedAtUtc;
                    }
                    
                    if (first.CapturedAtUtc < minMetricsDateTime)
                    {
                        minMetricsDateTime = first.CapturedAtUtc;
                    }
                }
            }
        }
        
        var didChange = (newValues != null && newValues.Count > 0)
                        || (trimmedValues != null && trimmedValues.Count > 0);

        if (didChange)
        {
            IReadOnlyList<MaudeMetric> added = newValues ?? Array.Empty<MaudeMetric>();
            IReadOnlyList<MaudeMetric> removed = trimmedValues as IReadOnlyList<MaudeMetric> ?? Array.Empty<MaudeMetric>();
            
            this.OnMetricsUpdated?.Invoke(this, new MaudeMetricsUpdatedEventArgs(added, removed));
        }
    }

    public void Event(string label)
    {
        Event(label, MaudeConstants.DefaultEventType, MaudeConstants.ReservedChannels.ChannelNotSpecified_Id, string.Empty);
    }

    public void Event(string label, MaudeEventType type)
    {
        Event(label, type, MaudeConstants.ReservedChannels.ChannelNotSpecified_Id, string.Empty);
    }

    public void Event(string label, MaudeEventType type, string details)
    {
        Event(label, type, MaudeConstants.ReservedChannels.ChannelNotSpecified_Id, details);
    }
    
    public void Event(string label, byte channel)
    {
        Event(label, MaudeConstants.DefaultEventType, channel, string.Empty);
    }

    public void Event(string label, MaudeEventType type, byte channel)
    {
        Event(label, type, channel, string.Empty);
    }

    public void Event(string label, MaudeEventType type, byte channel, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        details ??= string.Empty;
        
        MutateEvents(metrics =>
        {
            if (!metrics.TryGetValue(channel, out var channelEvents))
            {
                return Array.Empty<MaudeEvent>();
            }

            var @event = new MaudeEvent(label, type, details, DateTime.UtcNow, externalId: null, channel); 
            
            channelEvents.Add(@event);

            return new List<MaudeEvent>()
            {
                @event
            };
        });
    }
    
    
    private void MutateEvents(Func<Dictionary<byte, List<MaudeEvent>>, IReadOnlyList<MaudeEvent>> mutator)
    {
        if (mutator == null) throw new ArgumentNullException(nameof(mutator));

        IReadOnlyList<MaudeEvent> newValues = null;
        List<MaudeEvent>? trimmedValues = null;
        
        var expiryTime = DateTime.UtcNow - TimeSpan.FromSeconds(options.RetentionPeriodSeconds);
        
        lock (eventsLock)
        {
            newValues = mutator(eventsByChannel);

            foreach (var channel in eventsByChannel.Values)
            {
                var removeCount = 0;
                var initialCount = channel.Count;

                while (removeCount < initialCount && channel[removeCount].CapturedAtUtc < expiryTime)
                {
                    removeCount++;
                }

                if (removeCount > 0)
                {
                    trimmedValues ??= new List<MaudeEvent>();
                    trimmedValues.AddRange(channel.GetRange(0, removeCount));
                    channel.RemoveRange(0, removeCount);
                }
                
                if (channel.Count > 0)
                {
                    var last = channel[^1];
                    var first = channel[0];

                    if (last.CapturedAtUtc > maxEventsDateTime)
                    {
                        maxEventsDateTime = last.CapturedAtUtc;
                    }
                    
                    if (first.CapturedAtUtc < minEventsDateTime)
                    {
                        minEventsDateTime = first.CapturedAtUtc;
                    }
                }
            }
        }
        
        var didChange = (newValues != null && newValues.Count > 0)
            || (trimmedValues != null && trimmedValues.Count > 0);

        if (didChange)
        {
            IReadOnlyList<MaudeEvent> added = newValues ?? Array.Empty<MaudeEvent>();
            IReadOnlyList<MaudeEvent> removed = trimmedValues as IReadOnlyList<MaudeEvent> ?? Array.Empty<MaudeEvent>();
            
            this.OnEventsUpdated?.Invoke(this, new MaudeEventsUpdatedEventArgs(added, removed));
        }
    }

    public void Clear()
    {
        List<MaudeMetric> removedMetrics;
        List<MaudeEvent> removedEvents;

        lock (metricsLock)
        {
            removedMetrics = metricsByChannel.Values.SelectMany(m => m).ToList();
            foreach (var list in metricsByChannel.Values)
            {
                list.Clear();
            }

            minMetricsDateTime = DateTime.MaxValue;
            maxMetricsDateTime = DateTime.MinValue;
        }

        lock (eventsLock)
        {
            removedEvents = eventsByChannel.Values.SelectMany(e => e).ToList();
            foreach (var list in eventsByChannel.Values)
            {
                list.Clear();
            }

            minEventsDateTime = DateTime.MaxValue;
            maxEventsDateTime = DateTime.MinValue;
        }

        if (removedMetrics.Count > 0)
        {
            OnMetricsUpdated?.Invoke(this, new MaudeMetricsUpdatedEventArgs(Array.Empty<MaudeMetric>(), removedMetrics));
        }

        if (removedEvents.Count > 0)
        {
            OnEventsUpdated?.Invoke(this, new MaudeEventsUpdatedEventArgs(Array.Empty<MaudeEvent>(), removedEvents));
        }
    }
}
