
using System;

class PlaceDetail
{
  public string Name { get; internal set; }
  public string PlaceId { get; internal set; }
  public string FormattedAddress { get; internal set; }
  public Coordinate Location { get; internal set; }
  public Uri Uri { get; internal set; }
}
