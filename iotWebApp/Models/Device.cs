﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iotWebApp.Models
{
    public class IoTDevice:ISettings
    {
        public string DeviceID { get; set; }
        public string Connection { get; set; }
        public string DeviceName { get; set; }
        public double Reading { get; set; }
        public DateTime StartUp { get; set; }
        public string Firmware { get; set; }
        public DateTime CurrentTime { get; set; }
        public int Interval { get; set; }
        public double Range { get; set; }

        public DeviceReading GetReading()
        {
            var rand = new Random();
            CurrentTime = CurrentTime.AddSeconds(Interval);
            Reading += Range * (rand.NextDouble() - .5);
            return new DeviceReading
            {
                DeviceID = this.DeviceName,
                Reading = Reading,
                Time = CurrentTime
            };
        }

        public int UpdateFirmware(string firmware)
        {
            this.Firmware = firmware;
            return 0;
        }
        public ISettings GetSettings()
        {
            return this;
        }
        public int SetSettings(ISettings settings)
        {
            this.DeviceName = settings.DeviceName;
            this.Firmware = settings.Firmware;
            this.Interval = settings.Interval;
            return 0;
        }
        public DateTime Reboot()
        {
            this.StartUp = DateTime.Now;
            return this.StartUp;
        }
    }
    public interface ISettings
    {
        string DeviceID { get; set; }
        string DeviceName { get; set; }
        DateTime StartUp { get; set; }
        string Firmware { get; set; }
        int Interval { get; set; }

    }

    public class DeviceReading
    {
        public DateTime Time { get; set; }
        public string DeviceID { get; set; }
        public double Reading { get; set; }
    }

}
