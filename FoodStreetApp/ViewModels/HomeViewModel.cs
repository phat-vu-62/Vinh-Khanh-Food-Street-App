using System.Collections.ObjectModel;
using System.Windows.Input;
using FoodStreetApp.Models;

namespace FoodStreetApp.ViewModels
{
    public class HomeViewModel
    {
        public ObservableCollection<FoodPlace> FeaturedStalls { get; }
        public ObservableCollection<FoodPlace> PopularSeafoodStalls { get; }
        public ObservableCollection<FoodPlace> DrinksStalls { get; }

        public ICommand GoToDetailCommand { get; }

        public HomeViewModel()
        {
            FeaturedStalls = new ObservableCollection<FoodPlace>
            {
                new FoodPlace
                {
                    Name = "Ốc Phát",
                    Image = "ocphat.jpg",
                    Rating = 4.8,
                    Description = "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà.",
                    Latitude = 10.7621,
                    Longitude = 106.7032
                },
                new FoodPlace
                {
                    Name = "Ốc Hồng Nhung",
                    Image = "ochongnhung.jpg",
                    Rating = 4.5,
                    Description = "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng.",
                    Latitude = 10.7615,
                    Longitude = 106.7038
                },
                new FoodPlace
                {
                    Name = "BONA Food and Beer",
                    Image = "bona.jpg",
                    Rating = 4.9,
                    Description = "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm.",
                    Latitude = 10.7598,
                    Longitude = 106.7055
                }
            };

            PopularSeafoodStalls = new ObservableCollection<FoodPlace>
            {
                new FoodPlace
                {
                    Name = "Ốc Nhi 20k",
                    Image = "ocnhi20k.jpg",
                    Rating = 4.6,
                    Description = "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ.",
                    Latitude = 10.7610,
                    Longitude = 106.7042
                },
                new FoodPlace
                {
                    Name = "Ốc Ty",
                    Image = "octy.jpg",
                    Rating = 4.7,
                    Description = "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát.",
                    Latitude = 10.7605,
                    Longitude = 106.7050
                },
                new FoodPlace
                {
                    Name = "Lãng Quán",
                    Image = "langquan.jpg",
                    Rating = 4.4,
                    Description = "Hải sản tươi sống, không gian gia đình ấm cúng.",
                    Latitude = 10.7592,
                    Longitude = 106.7061
                }
            };

            DrinksStalls = new ObservableCollection<FoodPlace>
            {
                new FoodPlace
                {
                    Name = "Win - Trà Sữa - Coffee",
                    Image = "wincoffee.jpg",
                    Rating = 4.5,
                    Description = "Thức uống đa dạng, giải khát cực tốt sau khi ăn hải sản.",
                    Latitude = 10.7620,
                    Longitude = 106.7040
                }
            };

            GoToDetailCommand = new Command<FoodPlace>(async (place) =>
            {
                if (place == null) return;
                
                var navigationParameter = new Dictionary<string, object>
                {
                    { "FoodPlace", place }
                };

                await Shell.Current.GoToAsync("FoodDetailPage", navigationParameter);
            });
        }
    }
}