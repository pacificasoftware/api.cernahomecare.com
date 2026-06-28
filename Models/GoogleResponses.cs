namespace api.cernahomecare.com.Models
{
    using System.Text.Json.Serialization;

    public class GoogleGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("results")]
        public List<GoogleGeocodeResult> Results { get; set; } = new();
    }

    public class GoogleGeocodeResult
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("address_components")]
        public List<GoogleAddressComponent> AddressComponents { get; set; } = new();

        [JsonPropertyName("geometry")]
        public GoogleGeometry? Geometry { get; set; }
    }

    public class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string? LongName { get; set; }

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = new();
    }

    public class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleLocation? Location { get; set; }
    }

    public class GoogleLocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class ZipCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string City { get; set; } = "";
    }
}
