using Newtonsoft.Json;
using RestSharp;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Services
{
    public interface IDataService
    {
		Task<IReadOnlyCollection<BlogResponse>> GetBlogsAsync(int page);
		Task<BlogResponse> GetBlogAsync(string id);
		Task<IEnumerable<ExerciseQueryResponse>> GetExercisesByQueryAsync(string query);
		Task<bool> PostActionAsync(string token, MobileActions action, string data = null);
		Task<IEnumerable<YoutubeVideoItem>> GetVideosAsync(string exerciseName);
	}

    public class DataService : IDisposable, IDataService
    {
		private readonly RestClient _client;

		public DataService()
		{
            _client = new RestClient(new RestClientOptions("https://api.trainingday.space/api"));
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

		public async Task<IReadOnlyCollection<BlogResponse>> GetBlogsAsync(int page)
		{
			var cultureId = Settings.CultureName.Contains("en", StringComparison.OrdinalIgnoreCase) ? 1 : 2; 
			var request = CreateRequest($"/mobileblogs/blogs?cultureId={cultureId}&page={page}&pageSize=5", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<IReadOnlyCollection<BlogResponse>>(response.Content)
				: null;
		}

		public async Task<BlogResponse> GetBlogAsync(string id)
		{
			var request = CreateRequest($"/mobileblogs?id={id}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<BlogResponse>(response.Content)
				: null;
		}

		public async Task<IEnumerable<ExerciseQueryResponse>> GetExercisesByQueryAsync(string query)
		{
			var request = CreateRequest($"/exercises/query", Method.Post, new { Query = query });
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<IEnumerable<ExerciseQueryResponse>>(response.Content)
				: Enumerable.Empty<ExerciseQueryResponse>();
		}

		public async Task<bool> PostActionAsync(string token, MobileActions action, string data = null)
		{
			var model = new MobileAction { Action = action, Token = token, Data = data };
			var request = CreateRequest("/users/action", Method.Post, model);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful;
		}

		public async Task<IEnumerable<YoutubeVideoItem>> GetVideosAsync(string exerciseName)
		{
			var request = CreateRequest($"/YouTubeVideos/{exerciseName}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<IEnumerable<YoutubeVideoItem>>(response.Content)
				: Enumerable.Empty<YoutubeVideoItem>();
		}
	}
}
