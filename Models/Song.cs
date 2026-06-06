using CommunityToolkit.Mvvm.ComponentModel;

namespace WaveTuneNew.Models
{
    public enum Genre
    {
        Unknown,
        Trap,
        Rap,
        Hyperpop,
        NewJazz,
        CloudRap,
        HipHop,
        PopPunk,
    }

    public partial class Song : ObservableObject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public Genre Genre { get; set; }
        public int? AlbumId { get; set; }
        public Album? Album { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public List<User> LikedByUsers { get; set; } = new();
        public ImageSource PictureSource => ImageSource.FromFile(PictureUrl);

        [ObservableProperty]
        private bool isHovered;

        public string GenreDisplay => Genre switch
        {
            Genre.Trap => "Trap",
            Genre.Rap => "Rap",
            Genre.Hyperpop => "Hyperpop",
            Genre.NewJazz => "New Jazz",
            Genre.CloudRap => "Cloud Rap",
            Genre.HipHop => "Hip-Hop",
            Genre.PopPunk => "Pop-Punk",
            _ => string.Empty
        };

        public static Genre ParseGenre(string? value) => value?.ToLower().Trim() switch
        {
            "trap" => Genre.Trap,
            "rap" => Genre.Rap,
            "hyperpop" => Genre.Hyperpop,
            "new jazz" => Genre.NewJazz,
            "cloud rap" => Genre.CloudRap,
            "hip-hop" => Genre.HipHop,
            "hip hop" => Genre.HipHop,
            "pop-punk" => Genre.PopPunk,
            _ => Genre.Unknown
        };
    }
}