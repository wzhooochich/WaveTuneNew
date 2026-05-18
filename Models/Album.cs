namespace WaveTuneNew.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; } 
        public string PictureUrl { get; set; }
        public string Genre { get; set; }


        public List<Song> Songs { get; set; } 
    }
}

