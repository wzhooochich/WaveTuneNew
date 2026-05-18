namespace WaveTuneNew.Models
{
    public class UserLikedSong
    {
        public int UserId { get; set; }
        public int SongId { get; set; }
        public DateTime LikedAt { get; set; }


        public User? User { get; set; }
        public Song? Song { get; set; }
    }
}