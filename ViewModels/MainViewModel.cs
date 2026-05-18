using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WaveTuneNew.Models;

namespace WaveTuneNew.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        [RelayCommand]
        public async void GoToHome(string Path)
        {
            await Shell.Current.GoToAsync("//home");
        }

        [RelayCommand]
        public async void GoToAlbum(string Path)
        {
            await Shell.Current.GoToAsync("//AlbumPage");
        }

        /*internal class ProtoSongViewModel
        {
            public ObservableCollection<Song> Items { get; set; }

            public ProtoSongViewModel()
            {
                Items = new ObservableCollection<Song>()
            {
                new Song{ Title = "0 tears", Author = "Kai angel", Album = "Damage", Picture = "damage.png"},
                new Song{ Title = "damage", Author = "Kai angel", Album = 4"Damage", Picture = "damage.png"},
                new Song{ Title = "basement", Author = "Kai angel", Album = "Damage", Picture = "damage.png"},
                new Song{ Title = "drive", Author = "Kai angel", Album = "Damage", Picture = "damage.png"},
                new Song{ Title = "sirens", Author = "Kai angel", Album = "Damage", Picture = "damage.png"},
            };
            }
        }*/


    }

}
