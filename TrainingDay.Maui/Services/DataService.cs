using Newtonsoft.Json;
using RestSharp;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Services
{
    public interface IDataService
    {
		Task<BlogsResponse> GetBlogsAsync(int page);
		Task<BlogResponse> GetBlogAsync(string id);
		Task<IEnumerable<ExerciseQueryResponse>> GetExercisesByQuery(string query);
		Task<bool> PostAction(string token, MobileActions action, string data = null);
		Task<IEnumerable<YoutubeVideoItem>> GetVideos(string exerciseName);
	}

    public class DataService : IDisposable, IDataService
    {
		private readonly RestClient _client;

		public DataService()
		{
			_client = new RestClient(new RestClientOptions("https://trainingday.space/api"));
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

		public async Task<BlogsResponse> GetBlogsAsync(int page)
		{
			var request = CreateRequest($"/blogs?page={page}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<BlogsResponse>(response.Content)
				: null;
		}

		public async Task<BlogResponse> GetBlogAsync(string id)
		{
			var request = CreateRequest($"/blogs?id={id}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<BlogResponse>(response.Content)
				: null;
		}

		public async Task<IEnumerable<ExerciseQueryResponse>> GetExercisesByQuery(string query)
		{
			var request = CreateRequest($"/exercises/query", Method.Post, new { Query = query });
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<IEnumerable<ExerciseQueryResponse>>(response.Content)
				: Enumerable.Empty<ExerciseQueryResponse>();
		}

		public async Task<bool> PostAction(string token, MobileActions action, string data = null)
		{
			var model = new MobileAction { Action = action, Token = token, Data = data };
			var request = CreateRequest("/users/action", Method.Post, model);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful;
		}

		public async Task<IEnumerable<YoutubeVideoItem>> GetVideos(string exerciseName)
		{
			var request = CreateRequest($"/exercises/video?name={exerciseName}", Method.Get);
			var response = await _client.ExecuteAsync(request);
			return response.IsSuccessful
				? JsonConvert.DeserializeObject<IEnumerable<YoutubeVideoItem>>(response.Content)
				: Enumerable.Empty<YoutubeVideoItem>();
		}
	}
}
