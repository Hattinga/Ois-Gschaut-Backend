using System.Net.Http.Json;

namespace OisGschaut.API.Services;

public class TvMazeService(HttpClient http)
{
    public async Task<List<TvMazeEpisode>> FetchEpisodesAsync(int showId)
    {
        var resp = await http.GetAsync($"/shows/{showId}/episodes");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<TvMazeEpisode>>() ?? [];
    }

    // Best-match search — returns first TVMaze show for the given title
    public async Task<TvMazeShowResult?> SearchShowAsync(string name)
    {
        var resp = await http.GetAsync($"/singlesearch/shows?q={Uri.EscapeDataString(name)}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<TvMazeShowResult>();
    }
}
