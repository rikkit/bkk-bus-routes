
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

class Coordinate {
  public double Lat { get; private set; }
  public double Lon { get; private set; }

  public Coordinate(double lat, double lon) {
    this.Lat = lat;
    this.Lon = lon;
  }
}

class SnapToRoadResult {
  public Coordinate Coordinate { get; internal set; }
  public string PlaceId { get; internal set; }
}

class PlaceDetail {
  public string Name { get; internal set; }
  public string PlaceId { get; internal set; }
  public string FormattedAddress { get; internal set; }
  public Coordinate Location { get; internal set; }
  public Uri Uri { get; internal set; }
}

class GMapsClient
{
  private HttpClient httpClient = new HttpClient();
  private string api_key;

  public GMapsClient(string api_key)
  {
    this.api_key = api_key;
  }

  public async Task<ICollection<SnapToRoadResult>> SnapToRoadsAsync(IEnumerable<Coordinate> coordinates) {
    var builder = new UriBuilder("https://roads.googleapis.com/v1/snapToRoads");
    var qs = HttpUtility.ParseQueryString("");
    qs["key"] = this.api_key;
    qs["path"] = String.Join("|", coordinates.Select(pair => $"{pair.Lat},{pair.Lon}"));
    builder.Query = qs.ToString();

    var httpResult = await httpClient.GetAsync(builder.ToString());
    var json = await httpResult.Content.ReadAsStringAsync();
    var jo = JObject.Parse(json);
    
    var results = jo["snappedPoints"].Children().Select(token => {
      var loc = token["location"];
      var coordinate = new Coordinate(loc["latitude"].Value<double>(), loc["longitude"].Value<double>());

      return new SnapToRoadResult
      {
        Coordinate = coordinate,
        PlaceId = token["placeId"].Value<string>(),
      };
    });

    return results.ToList();
  }

  public async Task<PlaceDetail> GetPlaceDetailAsync(string placeId) {
    var builder = new UriBuilder("https://maps.googleapis.com/maps/api/place/details/json");
    var qs = HttpUtility.ParseQueryString("");
    qs["key"] = this.api_key;
    qs["placeid"] = placeId;
    builder.Query = qs.ToString();

    var httpResult = await httpClient.GetAsync(builder.ToString());
    var json = await httpResult.Content.ReadAsStringAsync();
    var jo = JObject.Parse(json);

    var result = jo["result"];

    var name = result["name"].Value<string>();
    var resultPlaceId = result["place_id"].Value<string>();
    var loc = result["geometry"]["location"];
    var coordinate = new Coordinate(loc["lat"].Value<double>(), loc["lng"].Value<double>());
    var formattedAddress = result["formatted_address"].Value<string>();
    var uri = new Uri(result["url"].Value<string>());

    return new PlaceDetail
    {
      Name = name,
      PlaceId = resultPlaceId,
      FormattedAddress = formattedAddress,
      Location = coordinate,
      Uri = uri,
    };
  }
}