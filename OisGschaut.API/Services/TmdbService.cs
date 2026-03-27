using System.Net.Http.Json;

namespace OisGschaut.API.Services;

public class TmdbService(HttpClient http, IConfiguration config)
{
    private string ApiKey => config["Tmdb:ApiKey"]
        ?? throw new InvalidOperationException("TMDB API key not configured. Set Tmdb:ApiKey in appsettings.");

    public async Task<List<TmdbSearchItem>> SearchAsync(string query)
    {
        var url = $"/3/search/multi?query={Uri.EscapeDataString(query)}&api_key={ApiKey}&include_adult=false";
        var response = await http.GetFromJsonAsync<TmdbSearchResponse>(url);
        return response?.Results
            .Where(r => r.MediaType is "movie" or "tv")
            .ToList() ?? [];
    }

    public async Task<TmdbMovieDetails?> FetchMovieAsync(int tmdbId)
        => await http.GetFromJsonAsync<TmdbMovieDetails>($"/3/movie/{tmdbId}?api_key={ApiKey}");

    public async Task<TmdbTvDetails?> FetchTvAsync(int tmdbId)
        => await http.GetFromJsonAsync<TmdbTvDetails>($"/3/tv/{tmdbId}?api_key={ApiKey}");
}
