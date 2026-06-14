using System.Text.Json.Serialization;

namespace api.cernahomecare.com.Services;

public class GoogleGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GoogleGeocodingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(double Latitude, double Longitude, string City)?> GetLatLongFromZipAsync(string zipCode)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Google Maps API key is missing.");
        }

        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(zipCode)}&components=postal_code:{Uri.EscapeDataString(zipCode)}|country:US&key={Uri.EscapeDataString(apiKey)}";

        var response = await _httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(url);

        if (response == null || response.Status != "OK" || response.Results.Count == 0)
        {
            return null;
        }

        var firstResult = response.Results[0];
        var location = firstResult.Geometry.Location;

        var city =
            firstResult.AddressComponents.FirstOrDefault(x => x.Types.Contains("locality"))?.LongName
            ?? firstResult.AddressComponents.FirstOrDefault(x => x.Types.Contains("postal_town"))?.LongName
            ?? firstResult.AddressComponents.FirstOrDefault(x => x.Types.Contains("administrative_area_level_3"))?.LongName
            ?? firstResult.AddressComponents.FirstOrDefault(x => x.Types.Contains("administrative_area_level_2"))?.LongName
            ?? "";

        return (location.Lat, location.Lng, city);
    }
}

public class GoogleGeocodeResponse
{
    [JsonPropertyName("results")]
    public List<GoogleGeocodeResult> Results { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}

public class GoogleGeocodeResult
{
    [JsonPropertyName("geometry")]
    public GoogleGeometry Geometry { get; set; } = new();

    [JsonPropertyName("address_components")]
    public List<GoogleAddressComponent> AddressComponents { get; set; } = new();
}

public class GoogleGeometry
{
    [JsonPropertyName("location")]
    public GoogleLocation Location { get; set; } = new();
}

public class GoogleLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public class GoogleAddressComponent
{
    [JsonPropertyName("long_name")]
    public string LongName { get; set; } = "";

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();
}