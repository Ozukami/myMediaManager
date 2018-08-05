using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace MediaManager
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private Dictionary<string, string> extensions = new Dictionary<string, string>();
        private List<Medias> selectionList = new List<Medias>();
        private string fileTypeFilter = "";

        public MainWindow()
        {
            InitializeComponent();
            LoadExtensions();
            Populate();
        }

        /*
         * Loads supported extensions from database
         */
        private void LoadExtensions()
        {
            using (var db = new mainEntities())
            {
                foreach (MediaTypes mediaType in ((from type in db.MediaTypes select type).ToList()))
                {
                    var extString = "";
                    foreach (MediaExtensions mediaExt in ((from ext in db.MediaExtensions where ext.MediaTypeID == mediaType.Id select ext).ToList()))
                    {
                        extString += "*." + mediaExt.Extension + ";";
                        extensions.Add(mediaExt.Extension, mediaType.Name);
                    }
                    extString = extString.Remove(extString.Length - 1);
                    fileTypeFilter += "|" + mediaType.Name + " files(" + extString + ")|" + extString;
                }
                if (fileTypeFilter.Length > 0)
                    fileTypeFilter = fileTypeFilter.Remove(0, 1);
                db.Dispose();
            }
        }

        /*
         * Loads saved medias from database
         */
        private void Populate()
        {
            List<Medias> mediaList = new List<Medias>();
            using (var db = new mainEntities())
            {
                Display((from media in db.Medias select media).ToList());
                db.Dispose();
            }
        }

        /*
         * Refreshes the listView where medias are displayed
         */
        private void Display(List<Medias> mediaList)
        {
            listView.Items.Clear();
            foreach (Medias media in mediaList)
            {
                listView.Items.Add(media);
            }
        }

        /*
         * Adds a media to the database
         */
        private void AddMediaButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = fileTypeFilter;
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    FileStream file = File.Open(openFileDialog.FileName, FileMode.Open);
                    using (var db = new mainEntities())
                    {
                        string fileExt = Path.GetExtension(file.Name).Remove(0, 1);
                        string fileType = extensions[fileExt];
                        Medias newMedia = new Medias
                        {
                            Name = Path.GetFileNameWithoutExtension(file.Name),
                            Ext = fileExt,
                            ExtID = (from ext in db.MediaExtensions where ext.Extension == fileExt select ext).Single<MediaExtensions>().Id,
                            Path = file.Name,
                            Type = fileType,
                            TypeID = (from type in db.MediaTypes where type.Name == fileType select type).Single<MediaTypes>().Id
                        };
                        db.Medias.Add(newMedia);
                        db.SaveChanges();
                        Populate();
                        file.Close();
                        db.Dispose();
                    }
                } catch (IOException) { }
            }
        }

        /*
         * Updates the selection list when a row is selected/unselected
         * And displays or plays the selected media
         */
        private void listView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (Medias m in e.AddedItems)
                {
                    selectionList.Add(m);
                }
                Medias media = (Medias)e.AddedItems[0];
                mediaElement.Source = new System.Uri(media.Path);
                mediaElement.Play();
                mediaControls.Visibility = (media.TypeID == 1) ? Visibility.Hidden : Visibility.Visible;
            }
            if (e.RemovedItems.Count > 0)
            {
                foreach (Medias m in e.RemovedItems)
                {
                    selectionList.Remove(m);
                }
            }
        }

        /*
         * Removes the selected medias from the database
         */
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new mainEntities())
            {
                foreach (Medias m in selectionList)
                {
                    db.Medias.RemoveRange(from media in db.Medias where media.Id == m.Id select media);
                }
                db.SaveChanges();
                selectionList.Clear();
                Populate();
                db.Dispose();
            }
        }

        /*
         * Removes every medias from the database
         */
        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new mainEntities())
            {
                db.Medias.RemoveRange(from media in db.Medias select media);
                db.SaveChanges();
                selectionList.Clear();
                Populate();
                db.Dispose();
            }
        }

        /*
         * Exits the application
         */
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /*
         * Plays the current selected media
         */
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
        }

        /*
         * Pauses the current playing media
         */
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
        }

        /*
         * Stops the current playing media
         */
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
        }
    }
}
