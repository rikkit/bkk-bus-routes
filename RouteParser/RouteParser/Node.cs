using System.Collections.Generic;

class Node
{
  public PlaceDetail Place { get; internal set; }

  public ICollection<Edge> Outbound { get; internal set; }

  public ICollection<Edge> Inbound { get; internal set; }
}
