namespace FingerprintReader.Base;

public class FingerprintConstants
{
    // Scanner command start bytes
    public const int StartCode = 0xEF01;

    // Packet identification        
    public const int CommandPacket = 0x01;
    public const int AckPacket = 0x07;
    public const int DataPacket = 0x02;
    public const int EndDataPacket = 0x08;

    // Instruction codes
    public const int VerifyPassword = 0x13;
    public const int GetSystemParameters = 0x0F;
    public const int CaptureFingerprint = 0x01;
   

    // Note: The documentation mean upload to host computer.
    public const int ConvertImage = 0x02;
    public const int CompareCharacteristics = 0x03;

    // Note: The documentation mean download from host computer.
    public const int UploadCharacteristics = 0x09;

    // Note: The documentation mean upload to host computer.
    public const int DownloadCharacteristics = 0x08;

    //Confirmation and Error codes
    public const int Ok = 0x00;
    public const int ErrorCommunication = 0x01;

    public const int ErrorWrongPassword = 0x13;

    public const int ErrorNoFinger = 0x02;
    public const int ErrorReadImage = 0x03;

    public const int ErrorMessyImage = 0x06;
    public const int ErrorFewFeaturePoints = 0x07;
    public const int ErrorInvalidImage = 0x15;
    public const int ErrorDownloadCharacteristics = 0x0D;

    // Unknown error codes
    public const int AddrCode = 0x20;

    // Char buffers        
    public const int CharBuffer1 = 0x01;
    public const int CharBuffer2 = 0x02;
}