namespace WaveTuneNew.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;


        public UserProfile? Profile { get; set; }
        public List<Song> Songs { get; set; } = new();
        public List<Song> LikedSongs { get; set; } = new();
    }

    public class UserProfile
    {
        public int UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;


        public User? User { get; set; }
    }
}
