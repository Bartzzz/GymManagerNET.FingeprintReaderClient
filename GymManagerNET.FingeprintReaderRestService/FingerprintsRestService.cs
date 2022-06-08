using GymManagerNET.Core.Models.DTOs.Subscriptions;
using GymManagerNET.FingeprintReaderRestService.Models;
using Newtonsoft.Json;
using RestSharp;

namespace GymManagerNET.FingeprintReaderRestService
{
    public class FingerprintsRestService
    {
        public List<FingerPrintDto> DatabaseFingerPrintsCache = new();
        private static string _baseUri;
        private static string _addFingerPrintResource = "addFingerprint/";
        private static string _getAllFingerPrints = "getAll/";
        private static string _verifySubscription = "verifyUser/";
        private readonly RestClient _client;
        

        public FingerprintsRestService(string baseUri)
        {
            _baseUri = baseUri;
            _client = new RestClient(_baseUri);
            UpdateDatabaseCache();
        }

        public void UpdateDatabaseCache()
        {
            DatabaseFingerPrintsCache = GetAllFingerPrints().ToList();
        }
        public FingerPrintDto AddFingerPrint(FingerPrintDto fingerPrint)
        {
            var request = new RestRequest(_addFingerPrintResource);
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(fingerPrint);
            var response = _client.Post(request);

            if (!response.IsSuccessful) throw new Exception($"Operation failed: {response.ErrorMessage}");

            var fingerPrintObject = JsonConvert.DeserializeObject<FingerPrintDto>(response.Content);
            return fingerPrintObject;
        }

        public IList<FingerPrintDto> GetAllFingerPrints()
        {
            var request = new RestRequest(_getAllFingerPrints);
            var response = _client.Get(request);

            if (!response.IsSuccessful) throw new Exception($"Operation failed: {response.ErrorMessage}");

            var fingerPrintObject = (JsonConvert.DeserializeObject<IEnumerable<FingerPrintDto>>(response.Content) ?? Array.Empty<FingerPrintDto>()).ToList();
            return fingerPrintObject;
        }

        public ActiveSubscriptionDto VerifySubscription(FingerPrintDto fprint)
        {
            var request = new RestRequest(_verifySubscription);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("userId", fprint.UserId, ParameterType.GetOrPost);
            var response = _client.Post(request);

            if (!response.IsSuccessful) throw new Exception($"Operation failed: {response.ErrorMessage}");

            var fingerPrintObject = JsonConvert.DeserializeObject<ActiveSubscriptionDto>(response.Content);
            return fingerPrintObject;
        }
    }
}