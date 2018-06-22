using System.Collections.Generic;

class BusService {
  public string Name { get; internal set; }

  public IList<Route> Routes { get; internal set; }

  public IList<PlaceDetail> Landmarks { get; internal set; }
}
