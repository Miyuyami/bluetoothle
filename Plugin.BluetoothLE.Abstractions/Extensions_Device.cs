﻿using System;
using System.Linq;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IDevice device, Guid serviceUuid, params Guid[] characteristicIds)
        {
            var distinctCharacteristicIds = characteristicIds.Distinct().ToArray();

            return device.GetKnownService(serviceUuid)
                         .SelectMany(x => x.GetKnownCharacteristics(distinctCharacteristicIds))
                         .Take(distinctCharacteristicIds.Length);
        }

        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device)
            => device.WhenServiceDiscovered().SelectMany(x => x.WhenCharacteristicDiscovered());


        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IDevice device)
            => device.WhenAnyCharacteristicDiscovered().SelectMany(x => x.WhenDescriptorDiscovered());


        public static bool IsPairingAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.PairingRequests);


        public static bool IsMtuRequestAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.MtuRequests);


        public static bool IsReliableTransactionsAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.ReliableTransactions);
    }
}
