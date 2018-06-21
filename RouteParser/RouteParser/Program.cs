using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;
using SharpKml.Dom;
using SharpKml.Engine;

namespace RouteParser
{
  class Program
  {
    const string RoutesFolder = "C:\\Users\\hello\\code\\bkk-bus-routes\\routes";
    static XNamespace RouteNS = "http://www.opengis.net/kml/2.2";

    static GMapsClient GMapsClient;

    static void Main(string[] args)
    {
      var config = JObject.Parse(File.ReadAllText("C:\\Users\\hello\\code\\bkk-bus-routes\\config.json"));
      GMapsClient = new GMapsClient(config["gmaps_api_key"].Value<string>());

      try
      {
        MainAsync().Wait();
      }
      finally
      {
        Console.WriteLine("Ended");
        Console.ReadLine();
      }
    }

    private static async Task MainAsync()
    {
      var files = Directory.GetFiles(RoutesFolder);
      foreach (var filePath in files)
      {
        await ParseFile(filePath);
      }
    }

    private static async Task ParseFile(string filePath)
    {
      using (var reader = File.OpenText(filePath))
      {
        var kml = KmlFile.Load(reader);
        var nodes = kml.Root.Flatten();
        var document = nodes.OfType<Document>().Single();
        Console.WriteLine($"Route: {document.Name}");

        var placemarks = nodes.OfType<Placemark>();
        foreach (var placemark in placemarks)
        {
          var lines = placemark.Geometry.Flatten().OfType<LineString>();
          foreach (var line in lines)
          {
            var coordinates = line.Coordinates.Select(pair => new Coordinate(pair.Latitude, pair.Longitude));
            var snapped = await GMapsClient.SnapToRoadsAsync(coordinates);
            var placeIds = snapped.Select(snappedPlace => snappedPlace.PlaceId).Distinct();

            var places = await Task.WhenAll(
              placeIds.Select(id => GMapsClient.GetPlaceDetailAsync(id))
            );
            
            // foreach (var place in places) {
            //   Console.WriteLine($"{place.Name} {place.Location.Lat},{place.Location.Lon}");
            // }

            // Sequential dedupe
            var placeNames = new Stack<string>();
            foreach (var place in places)
            {
              if (placeNames.Count > 0 && placeNames.Peek() == place.Name) {
                continue;
              }

              placeNames.Push(place.Name);
            }

            foreach (var placeName in placeNames) {
              Console.WriteLine(placeName);
            }
          }
        }

        Console.WriteLine();
      }
    }

  }
}
