using System.Collections.Generic;

class Route {
  public string Name { get; internal set; }

  public IList<PlaceDetail> Places { get; internal set; }
}
