using RestSharp;
using System.Text.Json;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Services
{
	public record ExerciseAiResponse(IEnumerable<ExerciseQueryResponse> Exercises);
	
    public interface IDataService
	{
		Task<IReadOnlyCollection<BlogResponse>> GetBlogsAsync(DateTimeOffset? createdOffset);

        Task<BlogResponse> GetBlogAsync(int id);
		Task<ExerciseAiResponse> GetExercisesByQueryAsync(string query);
		Task<bool> PostActionAsync(string token, MobileActions action);
		Task<bool> SendFirebaseTokenAsync(string token, string culture, string zone);

        Task<IEnumerable<YoutubeVideoItem>> GetVideosAsync(string exerciseName);
	}

    public class DataService : IDisposable, IDataService
    {
		private readonly RestClient _client;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

		public DataService()
		{
            _client = new RestClient(new RestClientOptions("https://api.trainingday.space/api")
			{
				Timeout= TimeSpan.FromSeconds(2)
			});
        }

        public void Dispose()
		{
			_client?.Dispose();
		}

		private static RestRequest CreateRequest(string resource, Method method, object body = null)
		{
			var request = new RestRequest(resource, method);
			if (body != null)
			{
				request.AddJsonBody(body);
			}
			
			return request;
		}

		public async Task<IReadOnlyCollection<BlogResponse>> GetBlogsAsync(DateTimeOffset? createdOffset)
		{
			var cultureId = Settings.CultureName.Contains("en", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

			string? createdQuery = createdOffset?.ToString("yyyy-MM-dd HH:mm:ss");

			var request = CreateRequest($"/mobileblogs?cultureId={cultureId}&createdFilter={createdQuery}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonSerializer.Deserialize<IReadOnlyCollection<BlogResponse>>(response.Content, _jsonOptions)
				: null;
		}

		public async Task<BlogResponse> GetBlogAsync(int id)
		{
			var request = CreateRequest($"/mobileblogs/{id}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonSerializer.Deserialize<BlogResponse>(response.Content, _jsonOptions)
				: null;
		}

		public async Task<ExerciseAiResponse> GetExercisesByQueryAsync(string query)
		{
			var request = CreateRequest($"/exercises/query", Method.Post, new { Query = query });
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonSerializer.Deserialize<ExerciseAiResponse>(response.Content, _jsonOptions)
				: new ExerciseAiResponse([]);
		}

        public async Task<bool> SendFirebaseTokenAsync(string token, string culture, string zone)
        {
            var model = new FirebaseTokenDto { Language = culture, Token = token, Zone = zone};
            var request = CreateRequest("/MobileTokens", Method.Post, model);
            var response = await _client.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> PostActionAsync(string token, MobileActions action)
		{
			var model = new MobileActionDto { Action = action, Token = token };
			var request = CreateRequest("/MobileTokens/action", Method.Post, model);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful;
		}

		public async Task<IEnumerable<YoutubeVideoItem>> GetVideosAsync(string exerciseName)
		{
			var request = CreateRequest($"/YouTubeVideos/{exerciseName}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonSerializer.Deserialize<IEnumerable<YoutubeVideoItem>>(response.Content, _jsonOptions)
				: [];
		}
	}
}
