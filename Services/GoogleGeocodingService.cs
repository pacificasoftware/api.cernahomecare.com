using Models;
using System.Net.Http;
using System.Text.Json;
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

    public async Task<ZipCoordinates?> GetLatLongFromZipAsync(string zipCode)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        zipCode = zipCode.Trim();

        if (zipCode.Length != 5 || !zipCode.All(char.IsDigit))
        {
            return null;
        }

        var urls = new[]
        {
        $"https://maps.googleapis.com/maps/api/geocode/json?components=country:US|postal_code:{Uri.EscapeDataString(zipCode)}&key={Uri.EscapeDataString(apiKey)}",
        $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(zipCode + ", USA")}&key={Uri.EscapeDataString(apiKey)}"
    };

        foreach (var url in urls)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    continue;
                }

                var results = root.GetProperty("results");

                if (results.GetArrayLength() == 0)
                {
                    continue;
                }

                foreach (var result in results.EnumerateArray())
                {
                    bool hasMatchingPostalCode = false;
                    bool isUnitedStates = false;

                    if (!result.TryGetProperty("geometry", out var geometry) ||
                        !geometry.TryGetProperty("location", out var location))
                    {
                        continue;
                    }

                    var latitude = location.GetProperty("lat").GetDouble();
                    var longitude = location.GetProperty("lng").GetDouble();

                    string city = "";
                    string state = "";
                    string formattedAddress = "";

                    if (result.TryGetProperty("formatted_address", out var formattedAddressElement))
                    {
                        formattedAddress = formattedAddressElement.GetString() ?? "";
                    }

                    if (result.TryGetProperty("address_components", out var components))
                    {
                        foreach (var component in components.EnumerateArray())
                        {
                            var longName = component.GetProperty("long_name").GetString() ?? "";
                            var shortName = component.TryGetProperty("short_name", out var shortNameElement)
                                ? shortNameElement.GetString() ?? ""
                                : "";

                            var types = component.GetProperty("types")
                                .EnumerateArray()
                                .Select(t => t.GetString())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .ToList();

                            // Verify ZIP matches exactly
                            if (types.Contains("postal_code") &&
                                longName.Equals(zipCode, StringComparison.OrdinalIgnoreCase))
                            {
                                hasMatchingPostalCode = true;
                            }

                            // Verify country is US
                            if (types.Contains("country") &&
                                shortName.Equals("US", StringComparison.OrdinalIgnoreCase))
                            {
                                isUnitedStates = true;
                            }

                            if (types.Contains("country"))
                            {
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(city) &&
                                (
                                    types.Contains("locality") ||
                                    types.Contains("postal_town") ||
                                    types.Contains("administrative_area_level_3") ||
                                    types.Contains("administrative_area_level_2")
                                ))
                            {
                                city = longName.Replace(" County", "");
                            }

                            if (string.IsNullOrWhiteSpace(state) &&
                                types.Contains("administrative_area_level_1"))
                            {
                                state = longName;
                            }
                        }
                    }

                    // Must have exact ZIP match and be in the US
                    if (!hasMatchingPostalCode || !isUnitedStates)
                    {
                        continue;
                    }

                    var cityLabel = string.Join(", ", new[] { city, state }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                    if (string.IsNullOrWhiteSpace(cityLabel) &&
                        !string.IsNullOrWhiteSpace(formattedAddress) &&
                        !formattedAddress.Equals("United States", StringComparison.OrdinalIgnoreCase) &&
                        !formattedAddress.Equals("USA", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = formattedAddress
                            .Split(',')
                            .Select(x => x.Trim())
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x) &&
                                !x.Equals("USA", StringComparison.OrdinalIgnoreCase) &&
                                !x.Equals("United States", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (parts.Count >= 2)
                        {
                            cityLabel = $"{parts[0]}, {parts[1]}";
                        }
                        else if (parts.Count == 1)
                        {
                            cityLabel = parts[0];
                        }
                    }

                    if (string.IsNullOrWhiteSpace(cityLabel) ||
                        cityLabel.Equals("United States", StringComparison.OrdinalIgnoreCase) ||
                        cityLabel.Equals("USA", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return new ZipCoordinates
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        City = cityLabel
                    };
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

} 
 