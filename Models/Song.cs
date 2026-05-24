namespace WaveTuneNew.Models
{
    public enum Genre
    {
        Unknown,
        Trap,
        Hyperpop,
        NewJazz
    }

    public class Song
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

        public static Genre ParseGenre(string? value) => value?.ToLower().Trim() switch
        {
            "trap" => Genre.Trap,
            "hyperpop" => Genre.Hyperpop,
            "new jazz" => Genre.NewJazz,
            _ => Genre.Unknown
        };
    }
}