using System.Net.Http.Json;

namespace OisGschaut.API.Services;

public class TmdbService(HttpClient http, IConfiguration config)
{
    private string ApiKey => config["Tmdb:ApiKey"]
        ?? throw new InvalidOperationException("TMDB API key not configured. Set Tmdb:ApiKey in appsettings.");

    public async Task<List<TmdbSearchItem>> SearchAsync(string query)
    {
        var url = $"/3/search/multi?query={Uri.EscapeDataString(query)}&api_key={ApiKey}&include_adult=false";
        var resp = await http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return [];
        var response = await resp.Content.ReadFromJsonAsync<TmdbSearchResponse>();
        return response?.Results
            .Where(r => r.MediaType is "movie" or "tv")
            .ToList() ?? [];
    }

    public async Task<TmdbMovieDetails?> FetchMovieAsync(int tmdbId)
    {
        var resp = await http.GetAsync($"/3/movie/{tmdbId}?api_key={ApiKey}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<TmdbMovieDetails>();
    }

    public async Task<TmdbTvDetails?> FetchTvAsync(int tmdbId)
    {
        var resp = await http.GetAsync($"/3/tv/{tmdbId}?api_key={ApiKey}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<TmdbTvDetails>();
    }

    public async Task<List<TmdbSearchItem>> TrendingAsync()
    {
        var resp = await http.GetAsync($"/3/trending/all/week?api_key={ApiKey}");
        if (!resp.IsSuccessStatusCode) return [];
        var response = await resp.Content.ReadFromJsonAsync<TmdbSearchResponse>();
        return response?.Results
            .Where(r => r.MediaType is "movie" or "tv")
            .ToList() ?? [];
    }
}
