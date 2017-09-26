using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileEncrypt.Models;
using FileEncrypt.ViewModels.Commands;
using System.Windows.Threading;

namespace FileEncrypt.ViewModels
{
    public class MainWindowViewModel : BaseViewModel, IResetable
    {
        #region Constants, statics

        const int minPwdLength = 6;
        const int defaultSize = 4096;
        const int maxBuffSize = 65535;

        #endregion

        #region Other Fields

        private ManualResetEvent mreWorking;
        private ManualResetEvent mreStop;
        private long cryptedBytes, restoredBytes;
        private int secondsWorking;
        private DispatcherTimer timer;

        #endregion

        #region Properties

        private string path;
        public string Path
        {
            get => path;
            set
            {
                path = value;
                OnPropertyChanged("Path");
            }
        }

        private int sizeBuff;
        public string SizeBuff
        {
            get => sizeBuff.ToString();
            set
            {
                // check for correct size 
                // no more than 65K
                int.TryParse(value, out sizeBuff);
                if (sizeBuff == 0 || sizeBuff > maxBuffSize)
                    sizeBuff = defaultSize;
     
                OnPropertyChanged("SizeBuff");
            }
        }

        private double progress;
        public double Progress
        {
            get => progress;
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        private string speed;
        public string Speed
        {
            get => speed;
            set
            {
                if (speed != value)
                {
                    speed = $"{value} KB/s";
                    OnPropertyChanged("Speed");
                }
            }
        }

        private string log;
        public string Log
        {
            get => log;
            set
            {
                log += $"[{DateTime.Now.ToLongTimeString()}] {value}{Environment.NewLine}";
                OnPropertyChanged("Log");
            }
        }

        public ICommand StartCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand StopCommand { get; }

        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            SizeBuff = defaultSize.ToString();

            mreStop         = new ManualResetEvent(false);
            mreWorking      = new ManualResetEvent(false);

            OpenFileCommand = new SimpleCommand(FileBrowseDialog);
            StartCommand    = new AsyncCommand(Start, null, enable => !mreWorking.WaitOne(0));
            StopCommand     = new SimpleCommand(Cancel, enable => mreWorking.WaitOne(0) && !mreStop.WaitOne(0));
       
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1d) };
            timer.Tick += SpeedMonitor;
        }

        #endregion

        #region Methods

        // check for correct user input
        private void Validate(object param)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            if (!(param is PasswordBox pwd))
                throw new ArgumentNullException(nameof(pwd));

            if (pwd.Password.Length < minPwdLength)
                throw new FormatException(nameof(pwd));
        }

        private void SpeedMonitor(object sender, EventArgs e)
        {
            // calcing speed of encrypting
            secondsWorking++;
            var speedInKbPerSec = (double)(cryptedBytes + restoredBytes) / secondsWorking / 1024;
            Speed = Math.Round(speedInKbPerSec, 2).ToString();
            secondsWorking++;
        }

        private void FileBrowseDialog()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
                Path = ofd.FileName;
        }

        private void Encrypt(Stream stream, byte[] buff, byte[] key)
        {
            var fileSize = stream.Length;
            var onePercentInBytes = fileSize / 100;
            var percentBreakPoints = onePercentInBytes;

            var crypter = new StreamCrypt(new CryptXor());

            while (cryptedBytes < fileSize)
            {
                if (mreStop.WaitOne(0))
                {
                    // encrypting is cancelled
                    Decrypt(stream, buff, key);
                    break;
                }

                // ecnrypt data
                var count = crypter.Crypt(stream, cryptedBytes, buff, key);
                
                cryptedBytes += count;
                if (cryptedBytes >= percentBreakPoints)
                {
                    // move progress
                    Progress++;
                    percentBreakPoints += onePercentInBytes;
                }
            }
        }

        private void Decrypt(Stream stream, byte[] buff, byte[] key)
        {
            // encrypting is cancelled, need to restore datas

            var fileSize = stream.Length;
            var onePercentInBytes = fileSize / 100;
            var percentBreakPoints = onePercentInBytes;

            var crypter = new StreamCrypt(new CryptXor());

            while (restoredBytes < cryptedBytes)
            {
                // decrypt
                var count = crypter.Crypt(stream, restoredBytes, buff, key);

                restoredBytes += count;
                if (restoredBytes >= percentBreakPoints)
                {
                    // moving back progress
                    Progress--;
                    percentBreakPoints += onePercentInBytes;
                }
            }
        }

        private void Start(object param)
        {
            // start button clicked
            try
            {
                Validate(param);
                Log = "Encrypting started...";
                mreWorking.Set();
                timer.Start();

                // change and confuse the encripting key
                var key = 
                    System.Security.Cryptography
                    .MD5
                    .Create()
                    .ComputeHash(
                        Encoding.Default.GetBytes(
                            (param as PasswordBox)?.Password));

                var buff = new byte[sizeBuff];

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    Encrypt(fs, buff, key);

                // encryptiin success
                Log = "Encrypting finished";
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message, "Null refference", MessageBoxButton.OK, MessageBoxImage.Error);
                Log = ex.Message;
            }
            catch (FormatException)
            {
                // problems with password
                MessageBox.Show("Enter 6 or more symbols for key", "Incorrect encription key", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log = "Incorrect encription key";
            }
            catch (FileNotFoundException ex)
            {
                // problems with filepath
                MessageBox.Show(ex.Message, "Incorrect file path", MessageBoxButton.OK, MessageBoxImage.Stop);
                Log = "Incorrect file path";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Log = ex.Message;
            }
            finally
            {
                Reset();              
            }
        }

        private void Cancel()
        {
            mreStop.Set();
            Log = "Cancelling...";
        }

        public void Reset()
        {
            Progress = 0d;
            timer.Stop();
            cryptedBytes = 0;
            restoredBytes = 0;
            secondsWorking = 0;
            Speed = string.Empty;
            mreStop.Reset();
            mreWorking.Reset();
        }

        #endregion
    }
}
