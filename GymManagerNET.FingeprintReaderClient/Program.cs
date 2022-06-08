// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations.Schema;
using FingerprintReader.Base;
using GymManagerNET.FingeprintReaderRestService;
using GymManagerNET.FingeprintReaderRestService.Models;
using System.Configuration;

Console.WriteLine("Hello, World!");
var apiBaseUri = ConfigurationManager.AppSettings["ApiBaseUri"];
var restService = new FingerprintsRestService(apiBaseUri);
var fingerPrintScannerService = new FingerPrintService(restService);
var dbFingerprints = restService.GetAllFingerPrints().ToList();
Console.WriteLine("Fingerprint scanner for GymManagerNET demo client");
var option = string.Empty;

while (true)
{
    Console.WriteLine("Please choose your action");
    Console.WriteLine("1. Add fingerprint into the database");
    Console.WriteLine("2. Search for provided fingerprint in database and verify subscription");
    Console.WriteLine("3. Update fingerprints cache");
    option = string.Empty;
    option = Console.ReadLine();
    var opt = 0;
    while (!(int.TryParse(option, out opt) && opt is > 0 and <= 3))
    {
        option = Console.ReadLine();
    }

    if (opt == 1)
    {
        Console.WriteLine("Please provide user id, in demo its not added automatically");

        var id = Console.ReadLine();
        var userId = 0;

        while (!int.TryParse(id, out userId) && restService.DatabaseFingerPrintsCache.All(x => x.UserId != userId))
        {
            Console.WriteLine("Not a number or user already exists in the database");
            id = Console.ReadLine();
        }

        try
        {
            var fprint = fingerPrintScannerService.DownloadCharacteristicsFromCapturedImage(Console.WriteLine);
            restService.AddFingerPrint(new FingerPrintDto() { Fingerprint = Convert.ToHexString(fprint), UserId = 2 });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    if (opt == 2)
    {
        try
        {
            var fprint =
                fingerPrintScannerService.GetCharsAndSearchForMatchInDatabase(restService.DatabaseFingerPrintsCache, Console.WriteLine);
            if (fprint != null && !string.IsNullOrEmpty(fprint.Fingerprint))
            {
                restService.VerifySubscription(fprint);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    if (opt == 3)
    {
        try
        {
            restService.UpdateDatabaseCache();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    Console.WriteLine("\n\n\n");

}