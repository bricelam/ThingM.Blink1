using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace ThingM
{
    public sealed class Blink1 : IDisposable
    {
        private static readonly DeviceWatcher _watcher;
        private static readonly IDictionary<string, Blink1> _devices = new Dictionary<string, Blink1>();

        private readonly DeviceInformation _deviceInformation;
        private readonly Lazy<IAsyncOperation<HidDevice>> _device;

        static Blink1()
        {
            var selector = HidDevice.GetDeviceSelector(0xFF00, 0x0001, 0x27B8, 0x01ED);

            _watcher = DeviceInformation.CreateWatcher(selector);
            _watcher.Added += HandleDeviceAdded;
            _watcher.Updated += HandleDeviceUpdated;
            _watcher.Removed += HandleDeviceRemoved;
            _watcher.Start();
        }

        private Blink1(DeviceInformation deviceInformation)
        {
            _deviceInformation = deviceInformation;
            _device = new Lazy<IAsyncOperation<HidDevice>>(
                () => HidDevice.FromIdAsync(_deviceInformation.Id, FileAccessMode.ReadWrite));
        }

        public static event EventHandler<Blink1> DeviceAdded;
        public static event EventHandler<Blink1> DeviceRemoved;

        public static IEnumerable<Blink1> Devices
            => _devices.Values;

        public async Task<uint> SetColorAsync(Color color)
        {
            var device = await _device.Value;

            var report = device.CreateFeatureReport(1);

            var buffer = report.Data.ToArray();
            buffer[1] = (byte)'n';
            buffer[2] = color.R;
            buffer[3] = color.G;
            buffer[4] = color.B;

            report.Data = buffer.AsBuffer();

            return await device.SendFeatureReportAsync(report);
        }

        public void Dispose()
        {
            if (_device.IsValueCreated)
            {
                _device.Value.GetResults().Dispose();
            }
        }

        private static void HandleDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            var device = new Blink1(args);

            _devices.Add(args.Id, device);
            DeviceAdded?.Invoke(sender, device);
        }

        private static void HandleDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
            => _devices[args.Id]._deviceInformation.Update(args);

        private static void HandleDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var device = _devices[args.Id];

            _devices.Remove(args.Id);
            DeviceRemoved?.Invoke(sender, device);
        }
    }
}
