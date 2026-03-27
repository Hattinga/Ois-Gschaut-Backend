using System.Net.Http.Json;

namespace OisGschaut.API.Services;

public class TvMazeService(HttpClient http)
{
    public async Task<List<TvMazeEpisode>> FetchEpisodesAsync(int showId)
        => await http.GetFromJsonAsync<List<TvMazeEpisode>>($"/shows/{showId}/episodes") ?? [];
}
