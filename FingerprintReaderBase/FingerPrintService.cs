using System.ComponentModel.DataAnnotations;
using System.Security;
using System.Security.Cryptography;
using GymManagerNET.FingeprintReaderRestService;
using GymManagerNET.FingeprintReaderRestService.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;

namespace FingerprintReader.Base;

public class FingerPrintService
{
    private FingerPrintScanner _scanner;
    private FingerprintsRestService _restService;

    public FingerPrintService(FingerprintsRestService restService, string port = "COM7")
    {
        _scanner = new FingerPrintScanner(port);
        _restService = restService;
    }

    public byte[] DownloadCharacteristicsFromCapturedImage(Action<string> notify)
    {
        if (!_scanner.IsInitialized)
            throw new Exception("Fingerprint sensor not initialised");

        notify("Waiting for finger...");

        // Wait that finger is read
        while (!_scanner.CaptureFingerprint()) ;

        // Converts read image to characteristics and stores it in charbuffer 1
        _scanner.ConvertImage(FingerprintConstants.CharBuffer1);
        var response =_scanner.DownloadCharacteristics(FingerprintConstants.CharBuffer1);
        return response;
    }

    //Method that compares provided fingerprint with those stored in database, and picks the one with best score if it and score is bigger than 40. Otherwise returns empty object;
    public FingerPrintDto GetCharsAndSearchForMatchInDatabase(IEnumerable<FingerPrintDto> fingerPrints, Action<string> notify)
    {
        if (!_scanner.IsInitialized)
            throw new Exception("Fingerprint sensor not initialised");

        notify("Waiting for finger...");

        // Wait that finger is read
        while (!_scanner.CaptureFingerprint()) ;

        // Converts read image to characteristics and stores it in charbuffer 1
        notify($"Acquiring characteristics");
        _scanner.ConvertImage(FingerprintConstants.CharBuffer1);

        notify($"Searching database for a match");
        var scoreForUser = new List<Tuple<string, int>>();
        foreach (var fingerPrint in fingerPrints)
        {
            if (string.IsNullOrEmpty(fingerPrint.Fingerprint))
            {
                continue;
            }

            var decodedChars = Convert.FromHexString(fingerPrint.Fingerprint);

            _scanner.UploadCharacteristics(decodedChars, FingerprintConstants.CharBuffer2);
           
            var result = _scanner.CompareCharacteristics();
            scoreForUser.Add(new Tuple<string, int>(fingerPrint.UserId.ToString(), result));
        }
        
        var sortedResults = scoreForUser.OrderByDescending(x => x.Item2);
        if (sortedResults.Any())
        {
            var bestResult = sortedResults.First();
            if (bestResult.Item2 >= 40)
            {
                notify($"Match found! User {bestResult.Item1} with score {bestResult.Item2}");
                return fingerPrints.FirstOrDefault(x => x.UserId.ToString() == sortedResults.FirstOrDefault().Item1);
               
            }
        }
        notify($"No matching fingerprint in database!");
        return new FingerPrintDto();
    }
}