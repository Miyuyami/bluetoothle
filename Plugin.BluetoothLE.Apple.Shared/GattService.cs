﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        public CBService Service { get; }
        public CBPeripheral Peripherial { get; }


        public GattService(Device device, CBService native) : base(device, native.UUID.ToGuid(), native.Primary)
        {
            this.Peripherial = device.Peripheral;
            this.Service = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = characteristicIds.Distinct().ToDictionary<Guid, Guid, IGattCharacteristic>(x => x, _ => null);

                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.ServiceEquals(args.Service))
                        return;

                    foreach (var nch in this.Service.Characteristics)
                    {
                        var ch = new GattCharacteristic(this, nch);
                        if (characteristics.ContainsKey(ch.Uuid))
                        {
                            characteristics[ch.Uuid] = ch;
                            ob.OnNext(ch);
                        }
                    }

                    if (characteristics.All(kvp => kvp.Value != null))
                        ob.OnCompleted();
                });
                var uuids = characteristics.Select(kvp => kvp.Key.ToCBUuid()).ToArray();
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(uuids, this.Service);

                return () => this.Peripherial.DiscoveredCharacteristic -= handler;
            });


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = new Dictionary<Guid, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.ServiceEquals(args.Service))
                        return;

                    foreach (var nch in this.Service.Characteristics)
                    {
                        var ch = new GattCharacteristic(this, nch);
                        if (!characteristics.ContainsKey(ch.Uuid))
                        {
                            characteristics.Add(ch.Uuid, ch);
                            ob.OnNext(ch);
                        }
                    }
                });
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(this.Service);

                return () => this.Peripherial.DiscoveredCharacteristic -= handler;
            })
            .ReplayWithReset(this.Device
                .WhenStatusChanged()
                .Where(s => s == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.characteristicOb;
        }


        bool ServiceEquals(CBService service)
        {
            if (!this.Service.UUID.Equals(service.UUID))
                return false;

			if (!this.Peripherial.Identifier.Equals(service.Peripheral.Identifier))
                return false;

            return true;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattService;
            if (other == null)
                return false;

			if (!ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.Service.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}
