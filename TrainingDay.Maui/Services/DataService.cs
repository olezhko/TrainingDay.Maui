using Newtonsoft.Json;
using RestSharp;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Services
{
    public class BaseRest
    {
        public RestClient InitApiClient(string url)
        {
            var options = new RestClientOptions(url)
            {
            };

            var client = new RestClient(options);
            return client;
        }

        public void Dispose(RestClient client)
        {
            client.Dispose();
            client = null;
        }

        public RestRequest InitRequest(Method method = Method.Get, object body = null)
        {
            var request = new RestRequest();
            request.Method = method;
            if (body != null)
            {
                request.AddParameter("application/json", body, ParameterType.RequestBody);
            }
            return request;
        }
    }

    public interface IDataService
    {
        Task<IEnumerable<ExerciseQueryResponse>> GetExerciseCodesByQuery(string query);
        Task<IEnumerable<string>> GetYouTubeUrlsByExerciseName(string exerciseName);
    }

    public class DataService : BaseRest, IDataService
    {
        private const string BaseUrl = "https://api.trainingday.space/api";
        public async Task<IEnumerable<ExerciseQueryResponse>> GetExerciseCodesByQuery(string query)
        {
            var url = $"{BaseUrl}/exercises?query={query}";
            var response = await InitApiClient(url).ExecuteAsync(InitRequest());
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<IEnumerable<ExerciseQueryResponse>>(response.Content);
            }
            return Enumerable.Empty<ExerciseQueryResponse>();
        }
        public async Task<IEnumerable<string>> GetYouTubeUrlsByExerciseName(string exerciseName)
        {
            var url = $"{BaseUrl}/exercises/youtube?exerciseName={exerciseName}";
            var response = await InitApiClient(url).ExecuteAsync(InitRequest());
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<IEnumerable<string>>(response.Content);
            }
            return Enumerable.Empty<string>();
        }
    }
}
