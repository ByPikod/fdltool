using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Threading;
using bz2portable.BZip2;
using System.Diagnostics;

namespace FDLTool
{

    public partial class MainForm : Form
    {

        /*
         * Extensions that the software will look for.
         */
        public static string[] allowedExtensions = {
            ".ain",
            ".bsp",
            ".mdl",
            ".mp3",
            ".ogg",
            ".otf",
            ".pcf",
            ".phy",
            ".png",
            ".svg",
            ".vtf",
            ".vmt",
            ".vtx",
            ".vvd",
            ".ttf",
            ".wav"
        };
        /**
         * Once a matching file found it will be put here. And a timer will be taking them to the listbox.
         */
        public List<string> tempFiles = new List<string>();
        string tempLua = "";
        string loadingFolder;

        private static readonly HttpClient client = new HttpClient();

        public MainForm()
        {
            InitializeComponent();
        }

        private async void loadForm(object sender, EventArgs e) => await loadContributors();

        private async Task loadContributors()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            try
            {

                client.DefaultRequestHeaders.Add("User-Agent", "ByPikod-FDLTool");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                var response = await client.GetAsync("https://api.github.com/repos/BYPIKOD/FDLTOOL/contributors");
                var result = JsonConvert.DeserializeObject<IList<dynamic>>((await response.Content.ReadAsStringAsync()));

                foreach (var item in result)
                {
                    listBox2.Items.Add(item.login);
                }
            }
            catch (Exception e)
            {
                listBox2.Items.Add("Github request failed.");
            }
        }

        private void youtubeLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/c/pikod");
        }

        private void githubLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/ByPikod/FDLTool");
        }

        private void contributorLink(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/" + listBox2.SelectedItem.ToString());
        }

        private void chooseDirectory(object sender, EventArgs e)
        {
            
            inputDirectory.ShowDialog();

            if (inputDirectory.SelectedPath == string.Empty) return;

            linkLabel3.Text = inputDirectory.SelectedPath;
            button2.Enabled = true;
            groupBox1.Enabled = true;
        
        }

        private void chooseOutputDirectory(object sender, EventArgs e)
        {

            outputDirectory.ShowDialog();

            if (outputDirectory.SelectedPath == string.Empty) return;
            button1.Enabled = true;
            linkLabel4.Text = outputDirectory.SelectedPath;

        }

        bool searching = false;
        bool _cancelSearch = false;
        private void searchClick(object sender, EventArgs e)
        {

            if (searching)
            {
                _cancelSearch = true;
                return;
            }

            searching = true;
            button1.Enabled = false;
            linkLabel3.Enabled = false;
            button2.Text = "Cancel";
            label8.Text = "Loading...";
            richTextBox1.Text = "";
            progressBar1.Style = ProgressBarStyle.Marquee;

            listBox1.Items.Clear();
            tempFiles.Clear();

            searchWorker.RunWorkerAsync();
            searchTimer.Start();

        }

        private void searchBackground(object sender, DoWorkEventArgs e)
        {

            string directory = inputDirectory.SelectedPath == string.Empty ? "." : inputDirectory.SelectedPath;

            var dir = new DirectoryInfo(directory);
            loadFolder(dir);

            foreach (string item in tempFiles)
            {
                var fileName = item.Replace('\\', '/');
                string extension = Path.GetExtension(fileName);
                tempLua = tempLua + "resource.AddFile( \"" + fileName + "\" )" + "\n";
            }

        }

        private void searchTimer_Tick(object sender, EventArgs e)
        {

            if (!searching)
            {

                searchTimer.Enabled = false;
                return;

            }

            uploadTempFiles();
            label8.Text = "Loading folder: "+loadingFolder;
        
        }

        /**
         * Put the elements in tempFiles list to the list box.
         */
        private void uploadTempFiles()
        {

            int listBoxItemCount = listBox1.Items.Count;
            int itemCount = tempFiles.Count;

            for (int i = listBoxItemCount; i < itemCount; i++)
                listBox1.Items.Add(tempFiles[i]);

        }
       
        private void loadFolder(DirectoryInfo directory)
        {

            loadingFolder = directory.FullName;

            var files = directory.GetFilesByExtensions(allowedExtensions);

            foreach (FileInfo file in files)
            {

                if (_cancelSearch) return;
                tempFiles.Add(file.FullName.Substring(inputDirectory.SelectedPath.Length + 1));

            }

            DirectoryInfo[] dirs = directory.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {

                if (_cancelSearch) return;
                loadFolder(dir);

            }

        }

        private void searchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            if (_cancelSearch)
            {

                richTextBox1.Text = "";
                listBox1.Items.Clear();
                
            }
            else
            {

                uploadTempFiles();
                richTextBox1.Text = tempLua + "\n-- Created by FDLTool\n-- www.github.com/ByPikod/FDLTool";

            }

            label8.Text = "Finish";
            button2.Text = "Search Files (Generates Lua)";
            progressBar1.Style = ProgressBarStyle.Blocks;
            button1.Enabled = true;
            linkLabel3.Enabled = true;
            searching = false;
            _cancelSearch = false;

        }

        bool compressing = false;
        bool cancelCompress = false;
        string compressingState;
        int compressLevel;
        private void Compress(object sender, EventArgs e)
        {

            if (compressing) {

                cancelCompress = true;
                return;
            
            }

            compressLevel = trackBar1.Value;
            compressing = true;
            cancelCompress = false;
            button2.Enabled = false;
            linkLabel4.Enabled = false;
            button1.Text = "Cancel";
            progressBar1.Style = ProgressBarStyle.Marquee;

            compressWorker.RunWorkerAsync();
            compressTimer.Start();

        }

        private void compressTimer_Tick(object sender, EventArgs e)
        {

            if (!compressing)
            {
                compressTimer.Enabled = false;
                return;
            }
            label8.Text = compressingState;

        }

        private void compressWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            compressingState = "Folder duplicating...";

            DirectoryInfo source = new DirectoryInfo(inputDirectory.SelectedPath);
            DirectoryInfo target = new DirectoryInfo(outputDirectory.SelectedPath);

            if(target.GetFileSystemInfos().Length == 0)
            {
                var res = MessageBox.Show("Output folder is not empty. Are you sure ?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.No) return;
            }

            Copy(source, target);
            recursiveCompress(target);

        }

        public void Copy(DirectoryInfo source, DirectoryInfo target)
        {

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                
                if (cancelCompress) return;

                compressingState = "Copying: " + target.FullName;
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);

            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                
                if (cancelCompress) return;

                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                Copy(diSourceSubDir, nextTargetSubDir);

            }

        }

        public void recursiveCompress(DirectoryInfo target)
        {

            compressingState = "Compressing: " + target.FullName;

            var files = target.GetFilesByExtensions(allowedExtensions);

            foreach (FileInfo file in files)
            {

                if (cancelCompress) return;

                using (var inFs = file.OpenRead()) {
                    using (var outFs = File.Create(file.FullName + ".bz2"))
                    {
                        BZip2.Compress(inFs, outFs, true, compressLevel);
                    }
                }

                file.Delete();

            }

            var dirs = target.GetDirectories();

            foreach (var dir in dirs)
            {

                if (cancelCompress) return;
                recursiveCompress(dir);

            }

        }

        private void compressWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (cancelCompress)
            {

            }
            else
            {
                Process.Start(outputDirectory.SelectedPath);
            }

            compressing = false;
            label8.Text = "Finish";
            button1.Text = "Compress";
            progressBar1.Style = ProgressBarStyle.Blocks;
            button2.Enabled = true;
            linkLabel4.Enabled = true;

        }

    }
}
