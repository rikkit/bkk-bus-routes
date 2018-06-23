class Edge
{
  public BusService Service { get; internal set; }

  public Route Route { get; internal set; }

  public Node From { get; internal set; }

  public Node To { get; internal set; }
}
