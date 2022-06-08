using FingerprintReader.Base.Utilities;
using log4net;
using log4net.Config;
using System.IO.Ports;

namespace FingerprintReader.Base;

public class FingerPrintScanner
{
    public bool IsInitialized => _initialized;
    private static SerialPort _serialPort;
    private static uint _address;
    private readonly uint _password;
    private static bool _initialized = false;
    internal readonly ILog logger;
    internal FingerPrintScanner(string port = "COM7", uint address = 0xFFFFFFFF, uint password = 0x00000000)
    {
        XmlConfigurator.Configure(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "ConfigFile\\Log4NetToFile.config")));
        logger = LogManager.GetLogger(typeof(FingerPrintScanner));
        _initialized = false;

        _serialPort = new SerialPort(port, 57600, Parity.None, 8);
        _address = address;
        _password = password;

        if (_serialPort.IsOpen)
            _serialPort.Close();
        _serialPort.ReadBufferSize = 50000;
        _serialPort.Open();
        _initialized = VerifyPassword();
        logger.Debug($"Scanner Initialized: {_initialized}");
    }


    internal Tuple<byte, List<byte>> SendBasicPacket(int packet, int commandName)
    {
        var command = new List<byte>()
        {
            Convert.ToByte(commandName)
        };

        var responseSize = WritePacket(packet, command);

        return ReadPacketByByte();
    }
    public bool VerifyPassword()
    {
        var command = new List<byte>
        {
            FingerprintConstants.VerifyPassword,
            (byte) RightShift(_password, 24),
            (byte) RightShift(_password, 16),
            (byte) RightShift(_password, 8),
            (byte) RightShift(_password, 0),
        };
        WritePacket(FingerprintConstants.CommandPacket, command.ToArray());
        var receivedPacket = ReadPacketByByte();

        byte receivedPacketType = receivedPacket.Item1;
        List<byte> receivedPacketPayload = receivedPacket.Item2;
        try
        {
            if (receivedPacketType != FingerprintConstants.AckPacket)
                throw new Exception("The received packet is no ack packet!");

            // DEBUG Sensor password is correct
            if (receivedPacketPayload[0] == FingerprintConstants.Ok)
                return true;

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorCommunication)
                throw new Exception("Communication error");

            if (receivedPacketPayload[0] == FingerprintConstants.AddrCode)
                throw new Exception("The address is wrong");

            // DEBUG Sensor password is wrong
            if (receivedPacketPayload[0] == FingerprintConstants.ErrorWrongPassword)
                return false;

            throw new Exception("Unknown error " + receivedPacketPayload[0].ToString("X"));
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            return false;
        }
    }

    internal int CompareCharacteristics()
    {
        int accuracyScore;
        var packetPayload = new List<byte> { FingerprintConstants.CompareCharacteristics };

        WritePacket(FingerprintConstants.CommandPacket, packetPayload.ToArray());
        var receivedPacket = ReadPacketByByte();

        var receivedPacketType = receivedPacket.Item1;
        var receivedPacketPayload = receivedPacket.Item2;

        if (receivedPacketType != FingerprintConstants.AckPacket)
            throw new Exception("The received packet is no ack packet!");

        // DEBUG Comparison successful
        if (receivedPacketPayload[0] == FingerprintConstants.Ok)
        {
            logger.Debug("comparison successful");
            accuracyScore = LeftShift(receivedPacketPayload[1], 8);
            accuracyScore = accuracyScore | LeftShift(receivedPacketPayload[2], 0);
            return accuracyScore;
        }

        logger.Debug("comparison unsuccessful");
        return 0;
    }

    internal ScannerParameters GetScannerParameters()
    {
        var result = SendBasicPacket(FingerprintConstants.CommandPacket, FingerprintConstants.GetSystemParameters);
        var packetType = result.Item1;
        var packetPayload = result.Item2;

        try
        {
            if (packetType != FingerprintConstants.AckPacket)
            {
                logger.Error("no ACK packet");
                throw new Exception("The received packet is no ack packet!");
            }

            // DEBUG Read successfully
            if (packetPayload[0] == FingerprintConstants.Ok)
            {
                var fingerprintParams = new ScannerParameters
                {
                    StatusRegister = LeftShift(packetPayload[1], 8) | LeftShift(packetPayload[2], 0),
                    SystemId = LeftShift(packetPayload[3], 8) | LeftShift(packetPayload[4], 0),
                    StorageCapacity = LeftShift(packetPayload[5], 8) | LeftShift(packetPayload[6], 0),
                    SecurityLevel = LeftShift(packetPayload[7], 8) | LeftShift(packetPayload[8], 0),
                    SensorAddress = (uint)((packetPayload[9] << 8 | packetPayload[10]) << 8 | packetPayload[11])
                        << 8 | packetPayload[12],
                    PacketLength = LeftShift(packetPayload[13], 8) | LeftShift(packetPayload[14], 0),
                    BaudRate = LeftShift(packetPayload[15], 8) | LeftShift(packetPayload[16], 0)
                };

                return fingerprintParams;
            }

            if (packetPayload[0] == FingerprintConstants.ErrorCommunication)
                throw new Exception("Communication error");

            throw new Exception("Unknown error " + packetPayload[0].ToString("X"));
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            return null;
        }

    }
    public bool CaptureFingerprint()
    {
        var response = SendBasicPacket(FingerprintConstants.CommandPacket, FingerprintConstants.CaptureFingerprint);
        var receivedPacketType = response.Item1;
        var receivedPacketPayload = response.Item2;

        try
        {
            if (receivedPacketType != FingerprintConstants.AckPacket)
                throw new Exception("The received packet is no ack packet!");

            // DEBUG Image read successful
            if (receivedPacketPayload[0] == FingerprintConstants.Ok)
                return true;

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorCommunication)
                throw new Exception("Communication error");

            // DEBUG No finger found
            if (receivedPacketPayload[0] == FingerprintConstants.ErrorNoFinger)
                return false;

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorReadImage)
                throw new Exception("Could not read image");

            throw new Exception("Unknown error " + receivedPacketPayload[0].ToString("X"));
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            return false;
        }
    }

    public bool ConvertImage(int charBufferNumber = FingerprintConstants.CharBuffer1)
    {
        var packetPayload = new List<byte>
        {
            FingerprintConstants.ConvertImage,
            (byte) charBufferNumber
        };

        WritePacket(FingerprintConstants.CommandPacket, packetPayload.ToArray());
        var receivedPacket = ReadPacketByByte();

        var receivedPacketType = receivedPacket.Item1;
        var receivedPacketPayload = receivedPacket.Item2;

        try
        {
            if (receivedPacketType != FingerprintConstants.AckPacket)
                throw new Exception("The received packet is no ack packet!");

            if (receivedPacketPayload[0] == FingerprintConstants.Ok)
                return true;

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorCommunication)
                throw new Exception("Communication error");

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorMessyImage)
                throw new Exception("The image is too messy");

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorFewFeaturePoints)
                throw new Exception("The image contains too few feature points");

            if (receivedPacketPayload[0] == FingerprintConstants.ErrorInvalidImage)
                throw new Exception("The image is invalid");


            throw new Exception("Unknown error " + receivedPacketPayload[0].ToString("X"));
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private List<byte> GetByteListBase()
    {
        var baseList = new List<byte>()
        {
            Convert.ToByte(RightShift(FingerprintConstants.StartCode, 8)),
            Convert.ToByte(RightShift(FingerprintConstants.StartCode, 0)),

            Convert.ToByte(RightShift(_address, 24)),
            Convert.ToByte(RightShift(_address, 16)),
            Convert.ToByte(RightShift(_address, 8)),
            Convert.ToByte(RightShift(_address, 0)),

        };
        return baseList;
    }

    private Tuple<byte, List<byte>> ReadPacketByByte()
    {
        var receivedPacketData = new List<byte>();

        int i = 0;

        while (true)
        {
            int responseByte = (byte)_serialPort.ReadByte();
            // Insert byte if packet seems valid

            receivedPacketData.Add((byte)responseByte);
            i++;

            if (i >= 12)
            {
                var packetPayloadLength = LeftShift(receivedPacketData[7], 8);
                packetPayloadLength = packetPayloadLength | LeftShift(receivedPacketData[8], 0);

                if (i < packetPayloadLength + 9)
                    continue;

                byte packetType = receivedPacketData[6];
                var packetPayload = new List<byte>();

                // Collect package payload (ignore the last 2 checksum bytes)
                for (var j = 9; j < 9 + packetPayloadLength - 2; j++)
                {
                    packetPayload.Add(receivedPacketData[j]);
                }

                return Tuple.Create(packetType, packetPayload);
            }
        }
    }

    private int WritePacket(int packet, IEnumerable<byte> command)
    {
        var packetType = Convert.ToByte(packet);
        var requestBytesList = GetByteListBase();
        var count = command.ToList().Count + 2;
        var checkSum = packetType + command.Sum(x => x) + RightShift(count, 8) + RightShift(count, 0);

        requestBytesList.Add(packetType);
        requestBytesList.Add(Convert.ToByte(RightShift(count, 8)));
        requestBytesList.Add(Convert.ToByte(RightShift(count, 0)));
        requestBytesList.AddRange(command);
        requestBytesList.Add(Convert.ToByte(RightShift(checkSum, 8)));
        requestBytesList.Add(Convert.ToByte(RightShift(checkSum, 0)));

        _serialPort.Write(requestBytesList.ToArray(), 0, requestBytesList.Count());

        return requestBytesList.ToArray().Length;
    }

    public byte[] DownloadCharacteristics(int charBufferNumber = FingerprintConstants.CharBuffer1)
    {
        if (charBufferNumber != FingerprintConstants.CharBuffer1 && charBufferNumber != FingerprintConstants.CharBuffer2)
            throw new Exception("The given char buffer number is invalid!");

        var packetPayload = new List<byte>
            {
                FingerprintConstants.DownloadCharacteristics,
                (byte) charBufferNumber
            };

        WritePacket(FingerprintConstants.CommandPacket, packetPayload.ToArray());
        var receivedPacket = ReadPacketByByte();

        var receivedPacketType = receivedPacket.Item1;
        var receivedPacketPayload = receivedPacket.Item2;

        if (receivedPacketType != FingerprintConstants.AckPacket)
            throw new Exception("The received packet is no ack packet!");

        // DEBUG The sensor will sent follow-up packets
        if (receivedPacketPayload[0] == FingerprintConstants.Ok) { }

        else if (receivedPacketPayload[0] == FingerprintConstants.ErrorCommunication)
            throw new Exception("Communication error");

        else if (receivedPacketPayload[0] == FingerprintConstants.ErrorDownloadCharacteristics)
            throw new Exception("Could not download characteristics");

        else
            throw new Exception("Unknown error " + receivedPacketPayload[0].ToString("X"));

        var completePayload = new List<byte>();

        // Get follow-up data packets until the last data packet is received
        while (receivedPacketType != FingerprintConstants.EndDataPacket)
        {
            receivedPacket = ReadPacketByByte();

            receivedPacketType = receivedPacket.Item1;
            receivedPacketPayload = receivedPacket.Item2;

            if (receivedPacketType != FingerprintConstants.DataPacket && receivedPacketType != FingerprintConstants.EndDataPacket)
                throw new Exception("The received packet is no data packet!");

            for (int i = 0; i < receivedPacketPayload.Count; i++)
            {
                completePayload.Add(receivedPacketPayload[i]);
            }
        }

        return completePayload.ToArray();
    }
    public bool UploadCharacteristics(byte[] characteristicsData, int charBufferNumber = FingerprintConstants.CharBuffer1)
    {
        if (charBufferNumber != FingerprintConstants.CharBuffer1 && charBufferNumber != FingerprintConstants.CharBuffer2)
            throw new Exception("The given char buffer number is invalid!");

        if (characteristicsData.Length < 1)
            throw new Exception("The characteristics data is required!");

        int maxPacketSize = GetMaxPacketSize();

        // Upload command
        var packetPayload = new List<byte>
            {
                FingerprintConstants.UploadCharacteristics,
                (byte) charBufferNumber
            };

        WritePacket(FingerprintConstants.CommandPacket, packetPayload.ToArray());
        var receivedPacket = ReadPacketByByte();

        var receivedPacketType = receivedPacket.Item1;
        var receivedPacketPayload = receivedPacket.Item2;

        if (receivedPacketType != FingerprintConstants.AckPacket)
            throw new Exception("The received packet is no ack packet!");

        // DEBUG The sensor will sent follow-up packets
        if (receivedPacketPayload[0] == FingerprintConstants.Ok) { }

        else if (receivedPacketPayload[0] == FingerprintConstants.ErrorCommunication)
            throw new Exception("Communication error");

        else
            throw new Exception("Unknown error " + receivedPacketPayload[0].ToString("X"));

        // Upload data packets
        int packetNumber = characteristicsData.Length / maxPacketSize;

        if (packetNumber <= 1)
            WritePacket(FingerprintConstants.EndDataPacket, characteristicsData);
        else
        {
            int lfrom, lto;
            int i = 1;
            while (i < packetNumber)
            {
                lfrom = (int)((i - 1) * maxPacketSize);
                lto = (int)(lfrom + maxPacketSize);
                WritePacket(FingerprintConstants.DataPacket, characteristicsData[lfrom..lto]);
                i += 1;
            }

            lfrom = (int)((i - 1) * maxPacketSize);
            lto = characteristicsData.Length;
            WritePacket(FingerprintConstants.EndDataPacket, characteristicsData[lfrom..lto]);
        }

        return true;
    }

    public int GetMaxPacketSize()
    {
        int packetMaxSizeType = GetScannerParameters().PacketLength;

        try
        {
            var packetSizes = new int[] { 32, 64, 128, 256 };
            int packetSize = packetSizes[packetMaxSizeType];
            return packetSize;
        }
        catch
        {
            throw new Exception("Invalid packet size");
        }
    }

    uint RightShift(uint n, int x) => n >> x & 0xFF;
    int RightShift(int n, int x) => n >> x & 0xFF;
    int LeftShift(int n, int x) => n << x;
}