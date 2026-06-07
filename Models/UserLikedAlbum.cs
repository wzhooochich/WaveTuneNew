namespace WaveTuneNew.Models
{
    public class UserLikedAlbum
    {
        public int UserId { get; set; }
        public int AlbumId { get; set; }
        public DateTime LikedAt { get; set; }

        public User? User { get; set; }
        public Album? Album { get; set; }
    }
}