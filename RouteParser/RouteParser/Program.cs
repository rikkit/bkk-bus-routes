using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SharpKml.Dom;
using SharpKml.Engine;

namespace RouteParser
{
  class Program
  {
    const string RoutesFolder = "C:\\Users\\hello\\code\\bkk-bus-routes\\routes";
    static XNamespace RouteNS = "http://www.opengis.net/kml/2.2";
    static HttpClient HttpClient = new HttpClient();
    static GMapsClient GMapsClient;

    static void Main(string[] args)
    {
      var config = JObject.Parse(File.ReadAllText("C:\\Users\\hello\\code\\bkk-bus-routes\\config.json"));
      GMapsClient = new GMapsClient(HttpClient, config["gmaps_api_key"].Value<string>());

      try
      {
        MainAsync().Wait();
      }
      catch (Exception e)
      {
        Console.WriteLine($"Program error: {e.Message}");

        if (Debugger.IsAttached)
        {
          throw;
        }
      }
      finally
      {
        Console.WriteLine("Ended");
        Console.ReadLine();
      }
    }

    private static async Task MainAsync()
    {
      await ScrapeRoutesAsync(RoutesFolder);

      var files = Directory.GetFiles(RoutesFolder);
      foreach (var filePath in files)
      {
        await ParseFile(filePath);
      }
    }

    private static async Task ScrapeRoutesAsync(string saveDirectory)
    {
      const string routesPostUrl = "https://bazztsu.blogspot.com/p/blog-page.html";

      string blogPageHtml = null;
      try
      {
        Console.WriteLine("Discovering routes...");
        var routesBlogPage = await HttpClient.GetAsync(routesPostUrl);
        blogPageHtml = await routesBlogPage.Content.ReadAsStringAsync();
      }
      catch (Exception e)
      {
        throw new Exception($"Couldn't get routes from blogpost - {e.Message}", e);
      }

      var htmlDoc = new HtmlDocument();
      htmlDoc.LoadHtml(blogPageHtml);

      var routesDiv = htmlDoc.GetElementbyId("post-body-2979590305454146467");
      var hrefNodes = routesDiv.SelectNodes("//a[@href]");

      var mapLinks = hrefNodes
        .Select(node => node.Attributes.Where(a => a.Name == "href").SingleOrDefault())
        .Where(node => node != null)
        .Select(node => new Uri(node.Value))
        .Where(uri =>
        {
          if (uri.Host != "www.google.com"
            && uri.Host != "mapsengine.google.com"
            && uri.Host != "drive.google.com")
          {
            Console.WriteLine($"Ignoring url: {uri.ToString()}");
            return false;
          }

          return true;
        })
        .ToList();

      if (mapLinks.Count == 0)
      {
        Console.WriteLine($"No links found at {routesPostUrl}");
        return;
      }
      else
      {
        var cachePath = Path.Combine(RoutesFolder, "routes-post.html");
        await File.WriteAllTextAsync(cachePath, blogPageHtml);
        Console.WriteLine($"Found {mapLinks.Count} routes at {routesPostUrl}");
        Console.WriteLine($"Saved copy of post at {cachePath}");
      }

      var mapIds = await Task.WhenAll(mapLinks.Select(link => GetMapIdFromUrl(link)));
      var distinctMapIds = mapIds.Where(id => !String.IsNullOrWhiteSpace(id)).Distinct().ToList();
      Console.WriteLine($"Downloading {distinctMapIds.Count} routes ({mapIds.Length - distinctMapIds.Count} dupes)");
      
      await Task.WhenAll(distinctMapIds.Select(id => DownloadGMapsKml(id, RoutesFolder)));
    }

    private static async Task<string> GetMapIdFromUrl(Uri uri)
    {
      // Redirects to url with mid param
      if (uri.Host == "drive.google.com")
      {
        // Autofollows 302
        var response = await HttpClient.GetAsync(uri);
        var destUri = response.RequestMessage.RequestUri;

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
          Console.WriteLine($"Not found: {uri}");
          return null;
        }
        else if (response.StatusCode != HttpStatusCode.OK || !destUri.ToString().Contains("google.com/maps"))
        {
          throw new Exception($"Followed drive url and didn't get expected maps link.\n\t{uri}\n\t{destUri}");
        }

        uri = destUri;
      }

      var qs = HttpUtility.ParseQueryString(uri.Query);
      var mapId = qs["mid"];

      return mapId;
    }

    private static async Task DownloadGMapsKml(string mapId, string saveFolder) {

      // https://www.google.com/maps/d/u/0/kml?mid={mapId}&forcekml=1
      var kmlUriBuilder = new UriBuilder("https://www.google.com/maps/d/u/0/kml");
      var kmlUriQs = HttpUtility.ParseQueryString("");
      kmlUriQs["forcekml"] = "1";
      kmlUriQs["mid"] = mapId;
      kmlUriBuilder.Query = kmlUriQs.ToString();

      var kml = await HttpClient.GetStringAsync(kmlUriBuilder.ToString());

      var outPath = Path.Combine(saveFolder, $"{mapId}.kml");
      await File.WriteAllTextAsync(outPath, kml);
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
              if (placeNames.Count > 0 && placeNames.Peek() == place.Name)
              {
                continue;
              }

              placeNames.Push(place.Name);
            }

            foreach (var placeName in placeNames)
            {
              Console.WriteLine(placeName);
            }
          }
        }

        Console.WriteLine();
      }
    }

  }
}
