namespace WaveTuneNew.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Genre { get; set; }


        public int? AlbumId { get; set; }
        public Album? Album { get; set; }


        public int? UserId { get; set; }
        public User? User { get; set; }


        public List<User> LikedByUsers { get; set; } = new();


        public ImageSource PictureSource => ImageSource.FromFile(PictureUrl);
    }
}
