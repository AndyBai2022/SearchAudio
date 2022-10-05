
using Microsoft.Win32;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Spire.Xls;
using Spire.Xls.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinForms = System.Windows.Forms;


namespace SearchAudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //string baseAudioPath = @"C:\Users\dc_bai\Desktop\ActivityAuto\activity\MakeSentence\audio\";
        string baseAudioPath = @"Z:\Users\Bai\SearchAudio\Files\audio\";


        // 防止播放声音时窗口假死
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        public MainWindow()
        {
        InitializeComponent();


            //在LoadingRow事件中设置Header，显示搜索结果序号
            SearchResultGrid.LoadingRow += (s, e) => e.Row.Header = e.Row.GetIndex()+1;
        }

       
        private void MouseDown_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


        private void playAudio(string audioRealPath, double beginTime, double endTime)
        {
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OnPlaybackStopped;

            if (audioFile == null)
            {
                audioFile = new AudioFileReader(audioRealPath);
                var trimmed = new OffsetSampleProvider(audioFile);
                trimmed.SkipOver = TimeSpan.FromSeconds(beginTime);
                trimmed.Take = TimeSpan.FromSeconds(endTime-beginTime);
                outputDevice.Init(trimmed);
            }
            try
            {
                outputDevice.Play();
            }
            catch(Exception e) 
            {
                //MessageBox.Show(e.Message);
            }

        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            outputDevice.Dispose();
            outputDevice = null;
            audioFile.Dispose();
            audioFile = null;
        }

        private void selectDataFileClick(object sender, RoutedEventArgs e)
        {
         
            WinForms.FolderBrowserDialog openFileDlg = new WinForms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                dataMapFilePath.Text = openFileDlg.SelectedPath;
            }

        }

        //选择文件夹，搜索文件夹下所有excel文件
        private async void BtnSearchClick(object sender, RoutedEventArgs e)
        {
            loading.Visibility = Visibility.Visible;
            if (!checkFolderSelected()) return;
            if (!checkKeyWordInput()) return;

            //ObservableCollection<Audio> searchResults = new ObservableCollection<Audio>();
            //ObservableCollection<Audio> searchResults = new ObservableCollection<Audio>();

            //如果根据word，则查找excel，路径中包含txt的话则去掉；如果根据sentence，则查找txt，路径中没有txt的话则加上
            dataMapFilePath.Text = (bool)bySentence.IsChecked && !dataMapFilePath.Text.Contains(@"\txt") ? dataMapFilePath.Text + @"\txt" : dataMapFilePath.Text;
            dataMapFilePath.Text = (bool)byWord.IsChecked && dataMapFilePath.Text.Contains(@"\txt") ? dataMapFilePath.Text.Replace(@"\txt", "") : dataMapFilePath.Text;
           
            //search by sentence, search in text files
            ObservableCollection<Audio> searchResults;
            if ((bool)bySentence.IsChecked)

            {
                Task<ObservableCollection<Audio>> searchTask = new Task<ObservableCollection<Audio>>(FindAudioBySentenceInFileList);
                searchTask.Start();
                searchResults = await searchTask;
            }
            //serarch by word, search in excel files
            else
            {
               
                Task<ObservableCollection<Audio>> searchTask = new Task<ObservableCollection<Audio>>(FindAudioByKeyWordInFileList);
                searchTask.Start();
                searchResults = await searchTask;
            }

            SearchResultGrid.DataContext = searchResults;
            
            loading.Visibility = Visibility.Collapsed;
           

        }

        private ObservableCollection<Audio> FindAudioBySentenceInFileList()
        {
            ObservableCollection<Audio> searchResults = new ObservableCollection<Audio>();

            //后台调用/刷新 UI须通过dispatcher.invoke
            Dispatcher.BeginInvoke(() =>
            {
                DirectoryInfo timemapFilePath = new DirectoryInfo(dataMapFilePath.Text.Trim());
                ObservableCollection<FileInfo> mapFileList = WalkDirectoryTree(timemapFilePath, "*.txt");
                string keywords = keyword.Text.Trim().ToLower();

                foreach (FileInfo file in mapFileList)
                {
                    string start, end;

                    string series = file.FullName.Split(@"timeinfo\")[1].Split(@"\")[0];//[1]为txt
                    string seriesFull = series == "osadb" ? "beginner" : series == "osader" ? "Early Reader" : series == "osads" ? "Science" : "Junior";
                    int bookNum = int.Parse(Regex.Match(file.Name, @"\d+").Value);
                    //matches匹配多个，match仅匹配第一项
                    int storyNum = int.Parse(Regex.Matches(file.Name, @"\d+")[1].Value);


                    if (char.IsPunctuation(keywords[^1])) keywords = keywords.Substring(0, keywords.Length - 2); //去掉后面的 标点符号

                    // int lineNumber = 0;
                    string text = File.ReadAllText(file.FullName);
                    //MessageBox.Show(text.ToLower());
                    //MessageBox.Show(keywords);
                    if (text.ToLower().Contains(keywords))
                    {

                        var sentences = text.Split($"{Environment.NewLine}");
                        for (int z = 0; z < sentences.Length; z++)
                        {
                            if (sentences[z].ToLower().Contains(keywords))
                            {
                                string context;
                                Audio foundAudio = new Audio();
                                foundAudio.Context = context = sentences[z].Split("$$$$")[0];//一整句话
                                foundAudio.KeyWords = keywords;
                                foundAudio.BookNumber = bookNum;
                                foundAudio.StoryNumber = storyNum;
                                foundAudio.EndTime = double.Parse(sentences[z].Split("$$$$")[1]);
                                foundAudio.BeginTime = z == 0 ? 0.0 : double.Parse(sentences[z - 1].Split("$$$$")[1]);
                                foundAudio.AudioPath = series;
                                foundAudio.Series = seriesFull;
                                context = context.ToLower();
                                var strarr = context.Split(keywords);
                                foundAudio.BeforeKeyWords = strarr[0];

                                string ak = "";
                                //keyword在句首
                                if (foundAudio.BeforeKeyWords.Length == 0)
                                {
                                    //MessageBox.Show(keywords.Length.ToString());
                                    ak = context.Substring(keywords.Length, context.Length - keywords.Length);
                                }
                                //keyword在句中
                                else
                                { 
                                    //去掉beforekeywords
                                    ak = context.Replace(foundAudio.BeforeKeyWords, ""); 
                                    //去掉第一次出现的关键字
                                    //ak的长度减去关键字的长度，substring 第二个参数是截取字符串的长度，而不是结束索引end index
                                    ak = ak.Substring(keywords.Length,ak.Length-keywords.Length);
                                }
                                //delete the first occurance of the keyword
                                //MessageBox.Show(ak);
                               // ak = ak.Remove(0,keywords.Length-1);
                                foundAudio.AfterKeyWords = ak;

                                searchResults.Add(foundAudio);
                                //MessageBox.Show(kwl.ToString());

                            }
                        }

                       
                    }

                    // show no record picture if # of the result list is 0
                    
                        norecord.Visibility = searchResults.Count == 0 ? Visibility.Visible : Visibility.Hidden;

                }
            });

            return searchResults;
        }




        //private ObservableCollection<Audio> FindAudioByKeyWordInFileList(ObservableCollection<FileInfo> mapFileList, string keyword)
        private ObservableCollection<Audio> FindAudioByKeyWordInFileList()
        {
            ObservableCollection<Audio> searchResults = new ObservableCollection<Audio>();

            Dispatcher.BeginInvoke(() =>
            {
                DirectoryInfo timemapFilePath = new DirectoryInfo(dataMapFilePath.Text.Trim());
                ObservableCollection<FileInfo> mapFileList = WalkDirectoryTree(timemapFilePath, "*.xlsx");

                foreach (var mapFile in mapFileList)
                {
                    SearchSingleExcelByKeyWord(mapFile, keyword.Text.Trim().ToLower(), ref searchResults);  //传递argument实参时也必须带上out修饰符
                }


                // show no record picture if # of the result list is 0
                norecord.Visibility = searchResults.Count == 0 ? Visibility.Visible : Visibility.Hidden;

            });
            //show the result
            return searchResults;



        }

        ///input parameters: fileName, keyword,打开excel搜索结果为一个单词
        ///output parameters: List<Audio>;
        private void SearchSingleExcelByKeyWord(FileInfo fileName, string keyword, ref ObservableCollection<Audio> searchResults)
        {
            //文件名为空或者文件不存在，退出
            if (string.IsNullOrEmpty(fileName.FullName) || !File.Exists(fileName.FullName)) return;
            if (string.IsNullOrEmpty(keyword)) return;

            keyword = keyword.Trim().ToLower();

            Workbook workBook = new Workbook();
            try
            {
                workBook.LoadFromFile(fileName.FullName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //open all worksheets 

            WorksheetsCollection worksheets = workBook.Worksheets;
            foreach (Worksheet sheet in worksheets) 
            {
                //如果用break或return 则会退出foreach，不再检查后面的sheet，如果evaluation后面还有其他sheet则会被遗漏
                //evaluation form 或者Word Summary(osadb)
                if (sheet.Name.Contains("Evaluation") || sheet.Name.Contains("Word")) continue;
                //MessageBox.Show($"{sheet.Name} in {fileName}");
                //如果最后一个字符为问号或者句号，则去掉后面的结束标点符号再查找
                if (keyword[^1] == '?' || keyword[^1] == '.') keyword = keyword.Substring(0, keyword.Length - 2).ToLower();
                //搜索sheet，，找到关键字以及时间，并返回

                //获取sheet的行数
                int rows = sheet.Rows.Length;
                //MessageBox.Show(rows.ToString());
                string words = "";

                //MessageBox.Show(sheet.Range[rows,1].Text);
                // return;
                Regex re = new Regex(@"\d+");
                Match m = re.Match(fileName.Name) ;
                int bookNumber = int.Parse(m.Value);
                int storyNumber = int.Parse(re.Match(sheet.Name).Value);

                var com = fileName.FullName.Split(@"\");
                //倒数第二个，osader, osadb, osad,osads
                string series = com[^2];

;
                string rowText = "";
                for (int i = 1; i <= rows; i++)
                {
                    //如果以句点，问号，感叹号结尾，则加上分隔符!@#，结束时间，newline
                    rowText = sheet.Range[i, 1].Text ?? "";
                    //如果最后一位是标点符号
                    rowText = StripPunctuation(rowText).ToLower();
                    //MessageBox.Show(rowText.ToLower());
                    if (rowText == keyword)
                        //MessageBox.Show($"找到{keyword}辣！在{series} book{ bookNumber}，story{ storyNumber} 第{ i}行, 开始时间{ sheet.Range[i, 3].Value}, 结束时间{ sheet.Range[i, 4].Value}");
                        searchResults.Add(new Audio(keyword,keyword, series,bookNumber,storyNumber, double.Parse(sheet.Range[i, 3].Value), double.Parse(sheet.Range[i,4].Value)));
                }

            }

        }

        private string StripPunctuation(string rowText)
        {
             var sb = new StringBuilder();
                foreach (char c in rowText)
                {
                    if (!char.IsPunctuation(c) || c=='-' || c=='\'')
                        sb.Append(c);
                }
                return sb.ToString();

        }

        //cut audio by starttime and end time
        void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
        {
          

            if (begin.HasValue && end.HasValue && begin > end)
                throw new ArgumentOutOfRangeException("end", "end should be greater than begin");

            using (var reader = new Mp3FileReader(inputPath))
            using (var writer = File.Create(outputPath))
            {
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                    if (reader.CurrentTime >= begin || !begin.HasValue)
                    {
                        if (reader.CurrentTime <= end || !end.HasValue)
                        { 
                        writer.Write(frame.RawData, 0, frame.RawData.Length);
                        
                    }
                    else break;
                    }
                
            }
           

            int channelNum = new AudioFileReader(outputPath).WaveFormat.Channels;
            //如果是stereo，则转成mono
            if (channelNum==2)
                ConvertStereoToMono(outputPath);
            else
            Process.Start("Explorer.exe", "/select," + outputPath);

        }

        private void ConvertStereoToMono(string outputPath)
        {
            using (var inputReader = new AudioFileReader(outputPath))
            {
                // convert our stereo ISampleProvider to mono

                var mono = new StereoToMonoSampleProvider(inputReader);
                mono.LeftVolume = 0.0f; // discard the left channel
                mono.RightVolume = 1.0f; // keep the right channel

                string wavPath = outputPath.Replace(".mp3",".wav");
                WaveFileWriter.CreateWaveFile16(wavPath, mono);
                //delete the original mp3 file
                File.Delete(outputPath);

                string newOutputPath = $"{outputPath.Replace(".mp3", "")}3.mp3";
                ConvertWavToMp3(wavPath, newOutputPath);
                
            }
        }

        private void ConvertWavToMp3(string wavPath, string outputPath)
        {

            using (var reader = new WaveFileReader(wavPath))
            {
                try
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, outputPath);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            File.Delete(wavPath);
            //open folder of the generated file
            Process.Start("Explorer.exe", "/select," + outputPath);

        }

        //search folder for excel files and return name list
        private ObservableCollection<FileInfo> WalkDirectoryTree(DirectoryInfo dir, string fileType) 
        {

            ObservableCollection<FileInfo> MapFileList = new ObservableCollection<FileInfo>();
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;
            try
            {
                files = dir.GetFiles(fileType);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {

                    //MessageBox.Show(fi.FullName);
                    MapFileList.Add(fi);

                }


                subDirs = dir.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, "*.xlsx");
                }
                
            }
            return MapFileList;

        }

        private bool checkFolderSelected()
        {
            if(!string.IsNullOrEmpty(dataMapFilePath.Text.Trim())) return true;
            return false;   
        }

        private bool checkKeyWordInput()
        {
            if (!string.IsNullOrEmpty(keyword.Text.Trim())) return true;
            return false;
        }

        private void playAudio(object sender, RoutedEventArgs e)
        {
            var ind = SearchResultGrid.SelectedIndex;
            Audio item = (Audio)(ind > -1 ? SearchResultGrid.Items[ind] : SearchResultGrid.Items[0]);
            string audioRealPath = $@"{baseAudioPath}{item.AudioPath}\book{item.BookNumber}\b{item.BookNumber}s{item.StoryNumber}.mp3";
            playAudio(audioRealPath, item.BeginTime, item.EndTime);
        }

        private void cutAudio(object sender, RoutedEventArgs e)
        {
            var ind = SearchResultGrid.SelectedIndex;
            Audio item = (Audio)(ind > -1 ? SearchResultGrid.Items[ind] : SearchResultGrid.Items[0]);
            string audioRealPath = $@"{baseAudioPath}{item.AudioPath}\book{item.BookNumber}\b{item.BookNumber}s{item.StoryNumber}.mp3";
            string fileName = StripPunctuation(item.KeyWords).Replace(" ","").ToLower();

            string basePath = $@"E:\temp\";

            //string basePath = $@"Z:\Projects\One Story A Day for Early Reader\Activity\audio\";
            //string basePath = $@"{Directory.GetCurrentDirectory()}\";
            string targetPath = (bool)bySentence.IsChecked ? $@"{basePath}osader_b{item.BookNumber}s{item.StoryNumber}a.mp3" : $@"{basePath}{item.KeyWords}.mp3" ;
            TimeSpan beginTime = TimeSpan.FromSeconds(item.BeginTime);
            TimeSpan endTime = TimeSpan.FromSeconds(item.EndTime);
            TrimMp3(audioRealPath,targetPath, beginTime, endTime);
        }



        public string CurrentDirectoryPath
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

       

     
    }
}
