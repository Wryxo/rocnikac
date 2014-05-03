﻿using DifferenceEngine;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Administration
{
    public partial class Form1 : Form
    {
        List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        System.IO.StreamWriter file;
        Hashtable changes = new Hashtable();
        string folderName;
        string folderPath;
        string package;
        string instType;
        string keyName = @"HKEY_CURRENT_USER\Software\SetItUp";
        string[] banned = new string[] {"cache", "cookies", "temp", "tmp", "appdata"};
        List<string> shortcuts = new List<string>();
        string[] packList;

        public Form1()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
            {
                return true;
            };
            folderName = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
            if (!Directory.Exists(folderName)) System.IO.Directory.CreateDirectory(folderName);
            using (WebClient myWebClient = new WebClient())
            {
                myWebClient.DownloadFile("https://localhost:44300/packages.txt", folderName+"\\packages.txt");
            }
            InitializeComponent();
            packList = File.ReadAllLines(folderName + "\\packages.txt");      
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void startFileWatchers()
        {
            package = textBox3.Text;
            folderPath = System.IO.Path.Combine(folderName, package);
            System.IO.Directory.CreateDirectory(folderPath);

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.DriveType == DriveType.Fixed) 
                { 
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    // Create a new FileSystemWatcher and set its properties.
                    watcher.Path = d.Name;
                    watcher.IncludeSubdirectories = true;
                    /* Watch for changes in LastAccess and LastWrite times, and
                       the renaming of files or directories. */
                    watcher.NotifyFilter = NotifyFilters.LastWrite
                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                    // Add event handlers.
                    watcher.Changed += new FileSystemEventHandler(OnChanged);
                    watcher.Created += new FileSystemEventHandler(OnChanged);
                    watcher.Deleted += new FileSystemEventHandler(OnChanged);
                    watcher.Renamed += new RenamedEventHandler(OnRenamed);

                    // Begin watching.
                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }
            }
        }

        // Define the event handlers. 
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            string[] words = e.FullPath.Split('\\');
            if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
            {
                bool clean = true;
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                if (clean && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg") && File.Exists(e.FullPath))
                {
                    writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                    changes.Add(e.FullPath, e.FullPath);
                }
            } else if (e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            {
                if (File.Exists(e.FullPath))
                {
                    bool nasiel = false;
                    bool clean = true;
                    foreach (string s in banned)
                    {
                        if (e.FullPath.ToLower().Contains(s)) clean = false;
                    }
                    if (clean && !nasiel && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg"))
                    {
                        writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                        if (!changes.Contains(e.FullPath)) changes.Add(e.FullPath, e.FullPath);
                    }
                }    
            }
        }

        private void copyTrackedFiles()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages")) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages");
            foreach (DictionaryEntry de in changes)
            {
                string line = (string)de.Value;
                string newPath = line.Substring(3);
                if (File.Exists(line)) { 
                    if (!Directory.Exists(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath))))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath)));
                    }
                    if (File.Exists(line))
                    {
                        if (line.Contains(".exe"))
                        {                            
                            string[] words = line.Split('\\');
                            Form2 dialog = new Form2();
                            dialog.StartPosition = FormStartPosition.CenterParent;
                            dialog.nazov(words[words.Length-1]);
                            if (dialog.ShowDialog(this) == DialogResult.OK)
                            {
                                /*writeToKonzole(dialog.shortcut() + Environment.NewLine);
                                var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                                IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages\\" + dialog.shortcut() + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                                shortcut.Arguments = package + " \"" + line + "\"";
                                shortcut.TargetPath = @"D:\\Bakalar\\UserApp\\UserApp\\bin\\Debug\\UserApp.exe";
                                shortcut.Save();*/
                                shortcuts.Add(dialog.shortcut() + " " + line);
                            }
                            else
                            {
                                writeToKonzole("Cancelled" + Environment.NewLine);
                            }
                        }
                        System.IO.File.Copy(line, System.IO.Path.Combine(folderPath, newPath), true);
                    }
                }
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            bool clean = true;
            if (File.Exists(e.FullPath))
            {
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                if (clean) {
                    writeToKonzole("File: " + e.OldFullPath + " renamed to " + e.FullPath + Environment.NewLine);
                    if (changes.Contains(e.OldFullPath)) changes.Remove(e.OldFullPath);
                    if (!changes.Contains(e.FullPath)) changes.Add(e.FullPath, e.FullPath);
                }
            }
        }

        public void ExportKey(string RegKey, string SavePath)
        {
            string path = "\"" + SavePath + "\"";
            string key = "\"" + RegKey + "\"";

            var proc = new Process();
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;
                proc = Process.Start("regedit.exe", "/e " + path + " " + key + "");

                if (proc != null) proc.WaitForExit();
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }

        }

        public void ImportKey(string SavePath)
        {
            string path = "\"" + SavePath + "\"";

            var proc = new Process();
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;
                proc = Process.Start("regedit.exe", path + "");

                if (proc != null) proc.WaitForExit();
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }

        }

        private string[] getResults(DiffList_TextFile destination, ArrayList DiffLines)
        {
            int i, j=1;
            string[] res = new string[4000];
            res[0] = "Windows Registry Editor Version 5.00";
            foreach (DiffResultSpan drs in DiffLines)
            {
                switch (drs.Status)
                {
                    case DiffResultSpanStatus.AddDestination:
                        for (i = 0; i < drs.Length; i++)
                        {
                            res[j] = ((TextLine)destination.GetByIndex(drs.DestIndex + i)).Line.ToString();
                            j++;
                        }
                        break;
                }
            }
            if (j == 1) return null;
            return res;
        }

        private void TextDiff(string sFile, string dFile)
        {
            DiffList_TextFile sLF = null;
            DiffList_TextFile dLF = null;
            try
            {
                sLF = new DiffList_TextFile(sFile);
                dLF = new DiffList_TextFile(dFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Error");
                return;
            }

            try
            {
                double time = 0;
                DiffEngine de = new DiffEngine();
                time = de.ProcessDiff(sLF, dLF, DiffEngineLevel.FastImperfect);
                ArrayList rep = de.DiffReport();
                string[] res = getResults(dLF, rep);
                if (res != null) File.WriteAllLines(folderPath + "\\" + package + ".reg", res);
            }
            catch (Exception ex)
            {
                string tmp = string.Format("{0}{1}{1}***STACK***{1}{2}",
                    ex.Message,
                    Environment.NewLine,
                    ex.StackTrace);
                MessageBox.Show(tmp, "Compare Error");
                return;
            }
        }

        private void writeToKonzole(string s)
        {
            textBox1.BeginInvoke((MethodInvoker)(() => textBox1.AppendText(s)));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            changes.Clear();
            shortcuts.Clear();
            button1.Enabled = false;
            startFileWatchers();
            ExportKey("HKEY_CURRENT_USER\\SOFTWARE", folderPath + "\\before.reg");
            writeToKonzole("Registre exportnute" + Environment.NewLine);
            button2.Enabled = true;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            foreach (FileSystemWatcher watcher in watchers) {
                watcher.EnableRaisingEvents = false;
            }
            Form3 dependDialog = new Form3();
            dependDialog.addData(packList);
            dependDialog.StartPosition = FormStartPosition.CenterParent;
            if (dependDialog.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllLines(folderPath + "\\depedencies.txt", dependDialog.getData());
            }
            Form4 instTypeDialog = new Form4();
            if (instTypeDialog.ShowDialog(this) == DialogResult.OK)
            {
                instType = "m";
            }
            else
            {
                instType = "a";
            }
            copyTrackedFiles();
            try
            { 
                file = new System.IO.StreamWriter(folderPath + "\\" + package + ".txt", false);
                foreach (DictionaryEntry de in changes)
                {
                    file.WriteLine(de.Value);
                }
                file.Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Nastala chyba v zapise do zoznamu");
            }
            try
            {
                file = new System.IO.StreamWriter(folderName + "\\packages.txt", true);
                file.WriteLine("p"+ instType + " " + package);
                foreach (string sc in shortcuts)
                {
                    file.WriteLine("s " + sc);
                }
                file.Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Nastala chyba v zapise do zoznamu");
            }
            writeToKonzole("Subory odkopirovane" + Environment.NewLine);
            ExportKey("HKEY_CURRENT_USER\\SOFTWARE", folderPath + "\\after.reg");
            writeToKonzole("Registre exportnute" + Environment.NewLine);
            TextDiff(folderPath + "\\before.reg", folderPath + "\\after.reg");
            writeToKonzole("Registre diffnute" + Environment.NewLine);
            File.Delete(folderPath + "\\after.reg");
            File.Delete(folderPath + "\\before.reg");
            button1.Enabled = true;
            writeToKonzole(package + " zaznamenany" + Environment.NewLine);
        }
    }
}
