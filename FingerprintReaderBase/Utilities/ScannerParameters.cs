using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FingerprintReader.Base.Utilities
{
    internal record ScannerParameters
    {
        // The status register (2 bytes)
        public int StatusRegister { get; set; }

        // The system id (2 bytes) 
        public int SystemId { get; set; }

        // The storage capacity (2 bytes)
        public int StorageCapacity { get; set; }

        // The security level (2 bytes)
        public int SecurityLevel { get; set; }

        // The sensor address (4 bytes)
        public uint SensorAddress { get; set; }

        // The packet length (2 bytes)
        public int PacketLength { get; set; }

        // The baud rate (2 bytes)
        public int BaudRate { get; set; }
    }
}
