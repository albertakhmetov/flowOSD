using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace flowOSD.Core.Hardware
{
    [Flags]
    public enum BatteryPowerState
    {
        PowerOnLine = 0x00000001,
        Discharging = 0x00000002,
        Charging = 0x00000004,
        Critical = 0x00000008
    }
}
