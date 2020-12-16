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

namespace FastDLGenerator
{
    public partial class Form1 : Form
    {
        public static string[] extList = {
            "*.bsp",
            "*.vmt",
            "*.vtf",
            "*.dx80.vtx",
            "*.dx90.vtx",
            "*.mdl",
            "*.phy",
            "*.sw.vtx",
            "*.vvd",
            "*.mp3",
            "*.wav",
            "*.pcf",
            "*.ttf",
            "*.png",
            "*.jpg"
        };

        public Form1()
        {
            InitializeComponent();
        }


        private void loadFolder(string directory)
        {
            foreach (string ext in extList)
            {
                String[] files = Directory.GetFiles(directory, ext);
                foreach (string file in files)
                {
                    listBox1.Items.Add(file);
                }
            }
            String[] dirs = Directory.GetDirectories(directory);
            foreach (string dir in dirs)
            {
                loadFolder(dir);
            }
        }

        private void executeFASTDL()
        {
            listBox1.Items.Clear();
            String[] dirs = Directory.GetDirectories(".\\");
            foreach (string dir in dirs)
            {
                loadFolder(dir);
            }
            richTextBox1.Text = "";
            foreach (string item in listBox1.Items)
            {
                string[] s = item.Split('\\');
                string x = item.Substring(s[0].Length+1);
                x = x.Replace('\\', '/');
                richTextBox1.Text = richTextBox1.Text + "resource.AddFile(\"" + x + "\")" + "\n";
            }
            richTextBox1.Text = richTextBox1.Text + "\n-- Created by Pikod FastDL Generator";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            executeFASTDL();
        }

    }
}
