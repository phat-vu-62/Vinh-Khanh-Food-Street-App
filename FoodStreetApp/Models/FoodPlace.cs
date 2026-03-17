namespace FoodStreetApp.Models
{
    public class FoodPlace
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public POI? PoiData { get; set; }
    }
}