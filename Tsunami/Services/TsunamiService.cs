using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public class TsunamiService
{
	private readonly HttpClient _httpClient;
	private readonly string GetRandomNumberUrl;
	private SemaphoreSlim semaphore;
	private long circuitStatus;
	private const long OPEN = 0;
	private const long TRIPPED = 1;
	public string UNAVAILABLE = "Unavailable";
	public bool _isGet;
	public StringContent _httpContent;

	public TsunamiService(string url,string token, int maxConcurrentRequests, bool isGet = true, StringContent content = null)
	{
		GetRandomNumberUrl = $"{url}";
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);		
		_isGet = isGet;
		if (!isGet) {
			_httpContent = content;
		}
		SetMaxConcurrency(url, maxConcurrentRequests);
		semaphore = new SemaphoreSlim(maxConcurrentRequests);

		circuitStatus = OPEN;
	}

	private void SetMaxConcurrency(string url, int maxConcurrentRequests)
	{
		ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = maxConcurrentRequests;
	}

	public void OpenCircuit()
	{
		if (Interlocked.CompareExchange(ref circuitStatus, OPEN, TRIPPED) == TRIPPED)
		{
			Console.WriteLine("Opened circuit");
		}
	}

	private void TripCircuit(string reason)
	{
		if (Interlocked.CompareExchange(ref circuitStatus, TRIPPED, OPEN) == OPEN)
		{
			Console.WriteLine($"Tripping circuit because: {reason}");
		}
	}

	private bool IsTripped()
	{
		return Interlocked.Read(ref circuitStatus) == TRIPPED;
	}
	public async Task<string> GetResponse()
	{
		try
		{
			await semaphore.WaitAsync();

			if (IsTripped())
			{
				return UNAVAILABLE;
			}
			HttpResponseMessage response;

			if (!_isGet)
			{
				 response = await _httpClient.PostAsync(GetRandomNumberUrl, _httpContent);
			}
			else {
				response = await _httpClient.GetAsync(GetRandomNumberUrl);
			}

			if (response.StatusCode != HttpStatusCode.OK)
			{
				TripCircuit(reason: $"Status not OK. Status={response.StatusCode}");
				return UNAVAILABLE;
			}

			return await response.Content.ReadAsStringAsync();
		}
		catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
		{
			Console.WriteLine("Timed out");
			TripCircuit(reason: $"Timed out");
			return UNAVAILABLE;
		}
		finally
		{
			semaphore.Release();
		}
	}
}