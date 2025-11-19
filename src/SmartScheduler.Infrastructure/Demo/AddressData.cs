using System.Text.Json;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Infrastructure.Demo;

/// <summary>
/// Represents an address location with timezone information.
/// </summary>
public record AddressLocation(
    string Address,
    string City,
    string State,
    string PostalCode,
    double Latitude,
    double Longitude,
    string Timezone,
    string FormattedAddress
);

/// <summary>
/// Loads and provides access to realistic US addresses from JSON data file.
/// </summary>
public class AddressData
{
    private readonly List<AddressLocation> _addresses;

    public AddressData()
    {
        _addresses = LoadAddresses();
    }

    public IReadOnlyList<AddressLocation> GetAllAddresses() => _addresses;

    public AddressLocation GetRandomAddress(Random random)
    {
        return _addresses[random.Next(_addresses.Count)];
    }

    public GeoLocation ToGeoLocation(AddressLocation address)
    {
        return new GeoLocation(
            address.Latitude,
            address.Longitude,
            address.Address,
            address.City,
            address.State,
            address.PostalCode,
            "US",
            address.FormattedAddress,
            null // No placeId for demo data
        );
    }

    private List<AddressLocation> LoadAddresses()
    {
        try
        {
            // Get the path to the JSON file
            var assemblyLocation = Path.GetDirectoryName(typeof(AddressData).Assembly.Location);
            var jsonPath = Path.Combine(assemblyLocation!, "Demo", "Data", "us-addresses.json");

            if (!File.Exists(jsonPath))
            {
                // Fallback: try relative path from current directory
                jsonPath = Path.Combine(AppContext.BaseDirectory, "Demo", "Data", "us-addresses.json");
            }

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Could not find us-addresses.json at {jsonPath}");
            }

            var json = File.ReadAllText(jsonPath);
            var addresses = JsonSerializer.Deserialize<List<AddressLocation>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return addresses ?? new List<AddressLocation>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load address data from us-addresses.json", ex);
        }
    }
}




