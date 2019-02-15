using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ABPRenamer.Resources;

namespace ABPRenamer
{

    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == Resource1.FormMain_BtnStart_Click_Run)
            {
                StartMethod();
            }
            else
            {
                StopMethod();
            }
        }
        private void StartMethod()
        {
            Arguments arguments = new Arguments
            {
                OldCompanyName = txtOldCompanyName.Text.Trim(),
                OldProjectName = txtOldProjectName.Text.Trim(),

                NewCompanyName = txtNewCompanyName.Text.Trim(),
                NewPeojectName = txtNewProjectName.Text.Trim()
            };
            if (string.IsNullOrEmpty(arguments.NewPeojectName))
            {
                MessageBox.Show(Resource1.Please_select_the_project_path, Resource1.Prompt, MessageBoxButtons.OK, MessageBoxIcon.Question);
                txtNewProjectName.Focus();
                return;
            }

            arguments.RootDir = txtRootDir.Text.Trim();
            if (string.IsNullOrWhiteSpace(arguments.RootDir))
            {
                if (DialogResult.Yes == MessageBox.Show(Resource1.Please_select_the_project_path, Resource1.Prompt, MessageBoxButtons.OK, MessageBoxIcon.Question))
                {
                    BtnSelect_Click(null, null);
                }
                return;
            }
            if (!Directory.Exists(arguments.RootDir))
            {
                MessageBox.Show(Resource1.Please_choose_the_correct_project_path);
                return;
            }

            //Show progress bar
            progressBar1.Visible = true;

            backgroundWorker1.RunWorkerAsync(arguments);
        }
        private void StopMethod()
        {
            if (backgroundWorker1.IsBusy)
            {
                MessageBox.Show(Resource1.Cancelling);
                backgroundWorker1.CancelAsync();
            }
        }
        private void Log(string value)
        {
            if (Console.InvokeRequired)
            {
                Action<string> act = (text) =>
                {
                    Console.AppendText(text);
                };
                Console.Invoke(act, value);
            }
            else
            {
                Console.AppendText(value);
            }

        }

        #region Worker event callback

        /// <summary>
        /// The callback method that the worker starts executing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker work = (BackgroundWorker)sender;
            Arguments arguments = e.Argument as Arguments;

            //Backup RootDir; when recursive, RootDir was modified
            if (arguments != null)
            {
                string backupRootDir = arguments.RootDir;


                Stopwatch sp = new Stopwatch();

                long spdir;

                sp.Start();

                RenameAllDir(work, e, arguments);
                sp.Stop();
                spdir = sp.ElapsedMilliseconds;

                Log($"================= Directory rename completed =================time consuming{spdir}(s)\r\n");

                sp.Reset();
                sp.Start();

                //Restore RootDir
                arguments.RootDir = backupRootDir;

                RenameAllFileNameAndContent(work, e, arguments);
                sp.Stop();
                Log($"================= File name and content renaming completed =================time consuming{sp.ElapsedMilliseconds}(s)\r\n");

                Log($"================= Completed =================Time-consuming catalog:{ spdir }s File time consuming:{ sp.ElapsedMilliseconds}s\r\n");
            }
        }
        /// <summary>
        /// Worker callback report return method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //e.UserState Send back the custom parameters passed by the report  

            Log(e.UserState.ToString());

            //Percentage of asynchronous tasks
            progressBar1.PerformStep();
        }
        /// <summary>
        /// Worker execution completed callback method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Restore the status of the start button
            btnStart.Text = Resource1.FormMain_BtnStart_Click_Run;

            if (e.Cancelled)
            {
                MessageBox.Show(Resource1.Task_terminated);
            }
            else if (e.Error != null)
            {
                MessageBox.Show(Resource1.Internal_error, e.Error.Message);
                throw e.Error;
            }
            else
            {
                if (DialogResult.Yes == MessageBox.Show(Resource1.RunWorkerCompleted, Resource1.Prompt, MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    BtnClose_Click(null, new MyEventArgs());
                }
            }

        }

        public class MyEventArgs : EventArgs
        {
            //This attribute is not used, using type judgment
            public bool IsCompleted { get; set; } = true;
        }

        #endregion       

        #region Recursively rename all directories

        /// <summary>
        /// Recursively rename all directories
        /// </summary>
        private void RenameAllDir(BackgroundWorker worker, DoWorkEventArgs e, Arguments arguments)
        {
            string[] allDir = Directory.GetDirectories(arguments.RootDir);

            int i = 0;
            foreach (string currDir in allDir)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {
                    arguments.RootDir = currDir;
                    RenameAllDir(worker, e, arguments);

                    DirectoryInfo dinfo = new DirectoryInfo(currDir);
                    if (dinfo.Name.Contains(arguments.OldCompanyName) || dinfo.Name.Contains(arguments.OldProjectName))
                    {
                        string newName = dinfo.Name;

                        if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                        {
                            newName = newName.Replace(arguments.OldCompanyName, arguments.NewCompanyName);
                        }
                        newName = newName.Replace(arguments.OldProjectName, arguments.NewPeojectName);

                        if (dinfo.Parent != null)
                        {
                            string newPath = Path.Combine(dinfo.Parent.FullName, newName);

                            if (dinfo.FullName != newPath)
                            {
                                //Send a report, here only the value of the progress is sent, and the second parameter can continue to send the relevant information.
                                worker.ReportProgress((i), $"{dinfo.FullName}\r\n=>\r\n{newPath}\r\n\r\n");
                                dinfo.MoveTo(newPath);
                            }
                        }
                    }

                } //Processing content ends

            }
        }

        #endregion

        #region Recursively rename all file names and file contents

        /// <summary>
        /// Recursively rename all file names and file contents
        /// </summary>
        private void RenameAllFileNameAndContent(BackgroundWorker worker, DoWorkEventArgs e, Arguments arguments)
        {
            //Get all files with the specified file extension in the current directory
            List<FileInfo> files = new DirectoryInfo(arguments.RootDir).GetFiles().Where(m => arguments.Filter.Contains(m.Extension)
                        && !m.FullName.Contains(".git")).ToList();

            int i = 0;
            //Rename current directory file and file content
            foreach (FileInfo item in files)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {
                    string text = File.ReadAllText(item.FullName, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                    {
                        text = text.Replace(arguments.OldCompanyName, arguments.NewCompanyName);
                    }

                    text = text.Replace(arguments.OldProjectName, arguments.NewPeojectName);

                    if (item.Name.Contains(arguments.OldCompanyName) || item.Name.Contains(arguments.OldProjectName))
                    {
                        string newName = item.Name;

                        if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                        {
                            newName = newName.Replace(arguments.OldCompanyName, arguments.NewCompanyName);

                        }
                        newName = newName.Replace(arguments.OldProjectName, arguments.NewPeojectName);
                        if (item.DirectoryName != null)
                        {
                            string newFullName = Path.Combine(item.DirectoryName, newName);

                            if (newFullName != item.FullName)
                            {
                                //Record file name changes
                                worker.ReportProgress(i, $"\r\n{item.FullName}\r\n=>\r\n{newFullName}\r\n\r\n");
                                File.Delete(item.FullName);
                            }
                            File.WriteAllText(newFullName, text, Encoding.UTF8);
                        }
                    }
                    else
                    {
                        File.WriteAllText(item.FullName, text, Encoding.UTF8);

                    }
                    worker.ReportProgress(i, $"{item.Name}=>完成\r\n");


                } //Processing content ends

            }
            //Rename current directory file and file content

            //Get subdirectory
            string[] dirs = Directory.GetDirectories(arguments.RootDir);
            foreach (string dir in dirs)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {

                    arguments.RootDir = dir;
                    RenameAllFileNameAndContent(worker, e, arguments);
                } //Processing content ends              
            }
            //Get subdirectory
        }

        #endregion

        #region Select file path
        /// <summary>
        /// Select file path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelect_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = Resource1.Please_select_the_folder_where_the_ABP_project_is_located
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, Resource1.Folder_path_cannot_be_empty, Resource1.Prompt);
                    return;
                }
                txtRootDir.Text = dialog.SelectedPath;
            }
        }

        #endregion

        #region Exit and save settings
        private void BtnClose_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(txtFilter.Text))
            {
                Settings1.Default.setFilter = txtFilter.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtOldCompanyName.Text))
            {
                Settings1.Default.setOldCompanyName = txtOldCompanyName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtOldProjectName.Text))
            {
                Settings1.Default.setOldProjectName = txtOldProjectName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtRootDir.Text))
            {
                Settings1.Default.setRootDir = txtRootDir.Text.Trim();
            }
            Settings1.Default.setNewCompanyName = txtNewCompanyName.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtNewProjectName.Text))
            {
                Settings1.Default.setNewProjectName = txtNewProjectName.Text.Trim();
            }

            if (e is MyEventArgs)
            {
                Settings1.Default.setOldCompanyName = txtNewCompanyName.Text.Trim();
                Settings1.Default.setOldProjectName = txtNewProjectName.Text.Trim();
            }

            Settings1.Default.Save();
            Environment.Exit(0);
        }
        #endregion

        #region Start load settings
        private void FormMain_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setFilter))
            {
                txtFilter.Text = Settings1.Default.setFilter.Trim(); ;
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setOldCompanyName))
            {
                txtOldCompanyName.Text = Settings1.Default.setOldCompanyName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setOldProjectName))
            {
                txtOldProjectName.Text = Settings1.Default.setOldProjectName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setRootDir))
            {
                txtRootDir.Text = Settings1.Default.setRootDir.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setNewCompanyName))
            {
                txtNewCompanyName.Text = Settings1.Default.setNewCompanyName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setNewProjectName))
            {
                txtNewProjectName.Text = Settings1.Default.setNewProjectName.Trim();
            }
        }
        #endregion

        #region Restore Defaults
        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtFilter.Text = Resource1.file_extention;
        }
        private void Label1_Click(object sender, EventArgs e)
        {
            txtOldCompanyName.Text = Resource1.OldCompanyName;
        }

        private void Label2_Click(object sender, EventArgs e)
        {
            txtOldProjectName.Text = Resource1.OldProjectName;
        }

        private void Label5_Click(object sender, EventArgs e)
        {
            txtNewCompanyName.Text = "";
        }

        private void Label4_Click(object sender, EventArgs e)
        {
            txtNewProjectName.Text = "";
        }

        private void Label3_Click(object sender, EventArgs e)
        {
            txtRootDir.Text = "";
        }
        #endregion

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {

        }
    }
    public class Arguments
    {
        public readonly string Filter = Resource1.file_extention;
        private string _oldCompanyName = Resource1.OldCompanyName;
        public string OldCompanyName
        {
            get => string.IsNullOrWhiteSpace(NewCompanyName) ? _oldCompanyName + "." : _oldCompanyName;
            set => _oldCompanyName = value;

        }
        public string OldProjectName { get; set; } = Resource1.OldProjectName;
        public string NewCompanyName { get; set; }
        public string NewPeojectName { get; set; }
        public string RootDir { get; set; }
    }
}
