class Coordinate
{
  public double Lat { get; private set; }
  public double Lon { get; private set; }

  public Coordinate(double lat, double lon)
  {
    this.Lat = lat;
    this.Lon = lon;
  }
}
