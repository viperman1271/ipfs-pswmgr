﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;

namespace Salus
{
    internal class FirstRunViewModel
    {
        #region Properties

        public ICommand NewAccount
        {
            get { return new DelegateCommand(() => OnNewAccount()); }
        }

        public ICommand ImportExisting
        {
            get { return new DelegateCommand(() => OnImportExisting()); }
        }

        #endregion

        #region Methods

        internal static bool WasCompleted()
        {
            if (!Directory.Exists(FileSystemConstants.IpfsConfigFolder))
                return false;

            if (!Directory.Exists(FileSystemConstants.PswmgrConfigFolder))
                return false;

            if (!File.Exists(Path.Combine(FileSystemConstants.IpfsConfigFolder, "keystore", "salus")))
                return false;

            EncryptionKey key = EncryptionKey.Load();
            if(key == null)
            {
                return false;
            }

            key.Dispose();
            key = null;

            return true;
        }

        private async void OnNewAccount()
        {
            if (!Directory.Exists(FileSystemConstants.IpfsConfigFolder))
            {
                InitIfps();
            }

            if (!File.Exists(Path.Combine(FileSystemConstants.PswmgrConfigFolder, "private.pem")))
            {
                using (EncryptionKey key = EncryptionKey.Generate())
                {
                    key.Export();
                }
            }

            if (!File.Exists(Path.Combine(FileSystemConstants.IpfsConfigFolder, "keystore", "salus")))
            {
                await IpfsApiWrapper.GenerateKeyPair("salus");
            }

            if (WasCompleted())
            {
                MessageBox.Show("Creation successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                OnAccountConfigued();
            }
            else
            {
                MessageBox.Show("There was an error creating the necessary files", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImportExisting()
        {
            InitIfps();

            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Backup Files (*.dat.bak)|*.dat.bak";
            if(fd.ShowDialog() == true)
            {
                string folderName = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
                while (Directory.Exists(folderName))
                {
                    folderName = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
                };
                Directory.CreateDirectory(folderName);

                ZipFile.ExtractToDirectory(fd.FileName, folderName);

                MoveFile(Path.Combine(folderName, "public.pem"), Path.Combine(FileSystemConstants.PswmgrConfigFolder, "public.pem"));
                MoveFile(Path.Combine(folderName, "private.pem"), Path.Combine(FileSystemConstants.PswmgrConfigFolder, "private.pem"));
                MoveFile(Path.Combine(folderName, "conf.json"), Path.Combine(FileSystemConstants.PswmgrConfigFolder, "conf.json"));
                MoveFile(Path.Combine(folderName, "salus"), Path.Combine(FileSystemConstants.IpfsConfigFolder, "keystore", "salus"));

                Directory.Delete(folderName, true);

                if (WasCompleted())
                {
                    MessageBox.Show("Import successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    OnAccountConfigued();
                }
                else
                {
                    MessageBox.Show("There was an error importing files", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static void MoveFile(string source, string destination)
        {
            Directory.CreateDirectory(Directory.GetParent(destination).FullName);

            if (File.Exists(destination))
                File.Delete(destination);
            File.Move(source, destination);
        }
        
        private static void InitIfps()
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("ipfs", "init");
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            process.WaitForExit();
        }

        #endregion

        #region Events

        public event EventHandler AccountConfigued;

        protected void OnAccountConfigued()
        {
            IpfsApiWrapper.StartDaemon();
            AccountConfigued?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
