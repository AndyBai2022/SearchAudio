using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAudio
{
    public class Audio
    {
        private string? keywords;
        private string? series;
        private string? desc; //shortcut of series, 'osader' for 'Early Reader', for example
        private int bookNumber;
        private int storyNumber;
        private double beginTime;
        private double endTime;
        private string audioPath;
  

        public string KeyWords
        {
            get;set;
        }
        public int BookNumber
        {
            get;set;
        }
        public int StoryNumber
        {
            get;set;
        }

        public string BeforeKeyWords { get; set; }
        public string AfterKeyWords { get; set; }

        public string Context {
            get;set;
        }
        public double BeginTime
        {
            get;set;
        }
        public double EndTime
        {
            get;set;
        }
        public string Series
        {
            get;set;
        }
        public enum SeriesShortcut
        {
            osad,
            osadb,
            osader,
            osads
        }
        public string? AudioPath
        {
            get;set;
        }
        
        public Audio()
        {

        }

        public Audio(string keywords,string context, string series, int bookNumber, int storyNumber, double beginTime, double endTime)
        {
            
            KeyWords = keywords;
            Context = context;
            Series = series;
            BookNumber = bookNumber;
            StoryNumber = storyNumber;
            BeginTime = beginTime;
            EndTime = endTime;
            BeforeKeyWords = Context.Split(KeyWords)[0];
            AfterKeyWords = Context.Split(KeyWords)[1];

            AudioPath = Series;
            //AudioPath = Series == "Beginner" ? SeriesShortcut.osadb.ToString() : Series == "Early Reader" ? SeriesShortcut.osader.ToString() : Series == "Junior" ? SeriesShortcut.osad.ToString() : Series == "Science" ? SeriesShortcut.osads.ToString():null;




        }

        public class ItemHandler
        {
            public ObservableCollection<Audio> Items { get; private set; }

            public ItemHandler()
            {
                Items = new ObservableCollection<Audio>();
            }

            public void Add(Audio item)
            {
                Items.Add(item);
            }
        }





        /// <summary>
        /// audio search results
        /// </summary>
        public class SearchResults : ObservableCollection<Audio>
        {
            private readonly ItemHandler _itemHandler;
        

            public ObservableCollection<Audio> Items
            {
                get { return _itemHandler.Items; }
            }


            public string CurrentDirectoryPath
            {
                get
                {
                    return Environment.CurrentDirectory;
                }
            }

            public SearchResults(string kw)
            {
                _itemHandler = new ItemHandler();
                //_itemHandler.Add(new Audio(kw, "Early Reader", 5, 15, 12.668, 13.354) { });
                //_itemHandler.Add(new Audio("today", "Beginner", 3, 22, 9.7, 10.1) { });
                //_itemHandler.Add(new Audio("birthday", "Early Reader", 5, 31, 15.274, 16.05) { });
                //_itemHandler.Add(new Audio("felt scared", "Early Reader", 5, 26, 37.282, 38.37) { });
                //_itemHandler.Add(new Audio("Uncle showed me how to bounce the ball", "Beginner", 1, 5, 12.8, 15.9) { });
                //_itemHandler.Add(new Audio("stop", "Beginner", 3, 30, 25.1, 25.9) { });
            }




        }

    }

}
