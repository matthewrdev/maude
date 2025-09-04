using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ansight.Adb.Devices;
using Ansight.Adb.Processes;
using Ansight.Adb.Telemetry.Sampling;
using Ansight.Concurrency;

namespace Maude.Runtime.Telemetry
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITelemetryManager))]
    [Export(typeof(IMutableTelemetryWatcher))]
    public class TelemetryManager : ITelemetryManager, IMutableTelemetryWatcher, IApplicationLifecycleHandler
    {
        readonly Logging.ILogger log = Logging.Logger.Create();

        private readonly Lazy<IAvailableDevicesWatcher> availableDevicesWatcher;
        public IAvailableDevicesWatcher AvailableDevicesWatcher => availableDevicesWatcher.Value;

        private readonly Lazy<IActiveProcessesWatcher> activeProcessesWatcher;
        public IActiveProcessesWatcher ActiveProcessesWatcher => activeProcessesWatcher.Value;

        private readonly Lazy<ITelemetryStreamFactoryRepository> telemetrySources;
        public ITelemetryStreamFactoryRepository TelemetrySources => telemetrySources.Value;

        private readonly Lazy<IDeviceDetailsService> deviceDetailsService;
        public IDeviceDetailsService DeviceDetailsService => deviceDetailsService.Value;

        private readonly ConcurrentValue<List<TelemetryObserver>> telemetryObservers = new ConcurrentValue<List<TelemetryObserver>>(new List<TelemetryObserver>());
        public IReadOnlyList<ITelemetrySink> TelemetrySinks => this.telemetryObservers.Get(value => value.Select(v => v.TelemetrySink).ToList()); // Create a shallow copy.

        [ImportingConstructor]
        public TelemetryManager(Lazy<IAvailableDevicesWatcher> availableDevicesWatcher,
                                Lazy<IActiveProcessesWatcher> activeProcessesWatcher,
                                Lazy<ITelemetryStreamFactoryRepository> telemetrySources,
                                Lazy<IDeviceDetailsService> deviceDetailsService)
        {
            this.availableDevicesWatcher = availableDevicesWatcher;
            this.activeProcessesWatcher = activeProcessesWatcher;
            this.telemetrySources = telemetrySources;
            this.deviceDetailsService = deviceDetailsService;
        }

        public event EventHandler<TelemetryWatchingStartedEventArgs> StartedWatchingTelemetry;

        public event EventHandler<TelemetryWatchingEndedEventArgs> StoppedWatchingTelemetry;

        public bool IsWatchingTelemetry(IDevice device, string packageId)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                return false;
            }

            return telemetryObservers.Get(value =>
            {
                return value.Any(observer => observer.DeviceId == device.Serial && observer.PackageId == packageId);
            });
        }

        public ITelemetrySink GetTelemetrySink(IDevice device, string packageId)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                return null;
            }

            return GetEditableTelemetrySink(device.Serial, packageId);
        }

        internal TelemetryObserver GetTelemetryObserver(string deviceId, string packageId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            return telemetryObservers.Get(value =>
            {
                return value.FirstOrDefault(observer => observer.DeviceId == deviceId && observer.PackageId == packageId);
            });
        }

        internal IMutableTelemetrySink GetEditableTelemetrySink(string deviceId, string packageId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            var observer = GetTelemetryObserver(deviceId, packageId);

            return observer?.TelemetrySink;
        }

        public ITelemetrySink StartWatching(IDevice device,
                                            string packageId,
                                            IReadOnlyList<string> channels)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            var isDeviceAvailable = AvailableDevicesWatcher.IsDeviceAvailable(device);
            if (!isDeviceAvailable)
            {
                throw new InvalidOperationException($"Unable to create a new telemetry sink for {device.Serial}-{packageId} as the device is not available.");
            }

            var deviceDetails = DeviceDetailsService.GetDeviceDetails(device);
            if (deviceDetails is null)
            {
                log?.Info($"Unable to start watching the telemetry for '{device.Serial}-{packageId} ' as the devices timezone could not be determined.");
                return null;
            }

            var observer = GetActiveObserverFor(device.Serial, packageId);
            if (observer != null)
            {
                return observer.TelemetrySink;
            }

            var telemetrySink = new TelemetrySink(device.Serial, packageId, isEditable: true);
            observer = new TelemetryObserver(device.Serial, packageId, deviceDetails.DeviceTimeOffset, telemetrySink, TelemetrySources, telemetryManager: this, channels);
            log?.Info($"Created Telemetry Observer to target the app '{packageId}' on the device '{device.Serial}' ");

            this.telemetryObservers.Mutate(value => value.Add(observer));

            TryActivateObserver(observer);

            return telemetrySink;
        }

        internal TelemetryObserver GetActiveObserverFor(string deviceId, string packageId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            return this.telemetryObservers.Get(value => value.FirstOrDefault(observer => observer.DeviceId == deviceId && observer.PackageId == packageId));
        }

        public void StopWatching(IDevice device, string packageId)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (packageId is null)
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            var observer = GetActiveObserverFor(device.Serial, packageId);
            if (observer is null)
            {
                return;
            }

            CloseObserver(observer, TelemetryWatchingEndedReason.UserRequested);
        }

        public void Startup()
        {
            AvailableDevicesWatcher.OnDevicesConnectionStateChanged += AvailableDevicesWatcher_OnDevicesChanged;
            AvailableDevicesWatcher.OnDevicesLost += AvailableDevicesWatcher_OnDevicesChanged;
            ActiveProcessesWatcher.ProcessesUpdated += ActiveProcessesWatcher_ProcessesUpdated;
        }

        public void Shutdown()
        {
            AvailableDevicesWatcher.OnDevicesConnectionStateChanged -= AvailableDevicesWatcher_OnDevicesChanged;
            AvailableDevicesWatcher.OnDevicesLost -= AvailableDevicesWatcher_OnDevicesChanged;
            ActiveProcessesWatcher.ProcessesUpdated -= ActiveProcessesWatcher_ProcessesUpdated;

            var observers = telemetryObservers.Get().ToList();

            foreach (var observer in observers)
            {
                CloseObserver(observer, TelemetryWatchingEndedReason.ApplicationStopped);
            }
        }

        private void ActiveProcessesWatcher_ProcessesUpdated(object sender, ActiveProcessesUpdatedEventArgs e)
        {
            var candidateObservers = this.telemetryObservers.Get(value => value.Where(o => o.DeviceId == e.Device.Serial).ToList());
            if (!candidateObservers.Any())
            {
                return;
            }

            var observers = telemetryObservers.Get();

            foreach (var observer in observers)
            {
                if (observer.DeviceId != e.Device.Serial)
                {
                    continue;
                }

                var isAppRunning = e.CurrentProcesses.HasProcessForPackage(observer.PackageId);
                var wasAppRunning = e.PreviousProcesses != null && e.PreviousProcesses.HasProcessForPackage(observer.PackageId);

                if (isAppRunning)
                {
                    if (observer.IsRunning == false)
                    {
                        log?.Info($"Activating the telemetry observer for '{observer.DeviceId}-{observer.PackageId}' as the application has started on the device.");
                        observer.Start();
                        StartedWatchingTelemetry?.Invoke(this, new TelemetryWatchingStartedEventArgs(observer.TelemetrySink));
                    }
                }
                else if (!isAppRunning && wasAppRunning)
                {
                    log?.Info($"Suspending the telemetry observer for '{observer.DeviceId}-{observer.PackageId}' as the application is no longer running.");
                    if (observer.IsRunning)
                    {
                        observer.Stop();
                    }

                    var sink = observer.TelemetrySink;
                    if (sink.IsEditable)
                    {
                        sink.CloseActiveSegments();
                    }
                }
            }
        }

        private void AvailableDevicesWatcher_OnDevicesChanged(object sender, DevicesChangedEventArgs e)
        {
            foreach (var device in e.Devices)
            {
                var isDeviceAvailable = this.AvailableDevicesWatcher.IsDeviceAvailable(device);
                if (isDeviceAvailable)
                {
                    continue;
                }

                var observers = this.telemetryObservers.Get(value => value.Where(o => o.DeviceId == device.Serial).ToList());
                if (!observers.Any())
                {
                    continue;
                }

                foreach (var observer in observers)
                {
                    CloseObserver(observer, TelemetryWatchingEndedReason.DeviceDisconnected);
                }
            }
        }

        private void CloseObserver(TelemetryObserver observer, TelemetryWatchingEndedReason reason)
        {
            if (observer is null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            log?.Info($"Closing the telemetry observer for '{observer.DeviceId}{observer.PackageId}'. Reason: {reason}");
            if (observer.IsRunning)
            {
                observer.Stop();
            }

            var sink = observer.TelemetrySink;
            sink.CloseEditing();
            this.telemetryObservers.Mutate(values => values.Remove(observer));

            this.StoppedWatchingTelemetry?.Invoke(this, new TelemetryWatchingEndedEventArgs(sink, reason));
        }

        private void TryActivateObserver(TelemetryObserver observer)
        {
            if (observer is null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            if (observer.IsRunning)
            {
                return;
            }

            var device = AvailableDevicesWatcher.GetDeviceWithSerial(observer.DeviceId);
            if (device is null || device.State != DeviceState.Connected)
            {
                log?.Info($"Unable to activate telemetry observer for '{observer.DeviceId}-{observer.PackageId}' as the device is not available");
                // Sanity check: should never happen.
                return;
            }

            var activeProcesses = this.ActiveProcessesWatcher.GetActiveProcesses(device);
            if (activeProcesses is null || !activeProcesses.Any())
            {
                log?.Info($"Unable to activate telemetry observer for '{observer.DeviceId}-{observer.PackageId}' as the telemetry watcher was unable to retieve the active processes state.");
                // Sanity check: should never happen.
                return;
            }

            var isPackageAvailable = activeProcesses.HasProcessForPackage(observer.PackageId);
            if (!isPackageAvailable)
            {
                log?.Info($"Unable to activate telemetry observer for '{observer.DeviceId}-{observer.PackageId}' as the package is not currently running");
                return;
            }

            log?.Info($"Activated the telemetry observer for '{observer.DeviceId}-{observer.PackageId}'");
            observer.Start();
            StartedWatchingTelemetry?.Invoke(this, new TelemetryWatchingStartedEventArgs(observer.TelemetrySink));
        }

        public void ActivateTelemetryChannel(IDevice device, string packageID, string channelName)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageID))
            {
                throw new ArgumentException($"'{nameof(packageID)}' cannot be null or empty.", nameof(packageID));
            }

            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentException($"'{nameof(channelName)}' cannot be null or empty.", nameof(channelName));
            }

            var observer = GetTelemetryObserver(device.Serial, packageID);
            if (observer is null)
            {
                return;
            }

            observer.AddChannel(channelName);
        }

        public void DeactivateTelemetryChannel(IDevice device, string packageID, string channelName)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageID))
            {
                throw new ArgumentException($"'{nameof(packageID)}' cannot be null or empty.", nameof(packageID));
            }

            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentException($"'{nameof(channelName)}' cannot be null or empty.", nameof(channelName));
            }

            var observer = GetTelemetryObserver(device.Serial, packageID);
            if (observer is null)
            {
                return;
            }

            observer.RemoveChannel(channelName);
        }

        public void ApplyTelemetryChannels(IDevice device, string packageID, IReadOnlyList<string> channels)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageID))
            {
                throw new ArgumentException($"'{nameof(packageID)}' cannot be null or empty.", nameof(packageID));
            }

            if (channels is null)
            {
                throw new ArgumentNullException(nameof(channels));
            }

            var observer = GetTelemetryObserver(device.Serial, packageID);
            if (observer is null)
            {
                return;
            }

            var existingChannels = observer.Channels;
            var added = channels.Except(existingChannels).ToList();
            var removed = existingChannels.Except(channels).ToList();

            foreach (var addedChannel in added)
            {
                observer.AddChannel(addedChannel);
            }

            foreach (var removedChannel in removed)
            {
                observer.RemoveChannel(removedChannel);
            }
        }

        public IReadOnlyList<string> GetWatchingTelemetryChannels(IDevice device, string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            var observer = GetTelemetryObserver(device.Serial, packageId);

            return observer?.Channels ?? Array.Empty<string>();
        }

        public void ReceivedTelemetrySamples(string deviceId,
                                             string packageId,
                                             string channel,
                                             IReadOnlyList<ITelemetrySample> samples)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or whitespace.", nameof(deviceId));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (samples is null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (!samples.Any())
            {
                return;
            }

            var telemetrySink = this.GetEditableTelemetrySink(deviceId, packageId);
            if (telemetrySink == null || !telemetrySink.IsEditable)
            {
                return;
            }

            var editableChannel = telemetrySink.GetEditableChannel(channel);
            if (editableChannel is null)
            {
                editableChannel = telemetrySink.CreateChannel(channel);
            }

            if (editableChannel == null || !editableChannel.IsEditable)
            {
                log?.Info($"Unable to add a new telemetry sample to channel '{channel}' as it is could not be found or is no longer editable");
                return;
            }

            foreach (var sample in samples)
            {
                if (!sample.Data.Any())
                {
                    continue;
                }

                var group = sample.Group;
                var segment = editableChannel.GetCurrentSegment(group);
                if (segment is null || !segment.IsEditable)
                {
                    segment = editableChannel.OpenSegmentForGroup(sample.Group, sample.CapturedAtUtc);
                }

                if (!segment.IsEditable)
                {
                    log?.Info($"Unable to add a new telemetry sample to segment '{segment.Group}|{segment.Id}' as it is no longer editable");
                    continue;
                }

                segment.AddData(sample.Data);
            }
        }
    }
}