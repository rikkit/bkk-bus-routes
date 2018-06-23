using System.Collections.Generic;

class ServiceNetwork
{
  /// <summary>
  /// All the nodes in the graph, keyed by their Google Maps place id.
  /// Each node links to a set of edges, which can be traversed to reach other nodes.
  /// </summary>
  public IReadOnlyDictionary<string, Node> Nodes { get; internal set; }
  
  /// <summary>
  /// A point to start traversing the network from.
  /// </summary>
  public Node Root { get; internal set; }

  /// <summary>
  /// The services used to build this graph.
  /// </summary>
  public IReadOnlyCollection<BusService> Services { get; internal set; }
}
