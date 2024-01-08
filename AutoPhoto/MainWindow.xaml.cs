using AutoPhoto.Models;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
using Color = System.Drawing.Color;
using InputType = AutoPhoto.Models.InputType;
using Point = System.Drawing.Point;
using Rect = AutoPhoto.Models.Rect;
using Size = System.Drawing.Size;
using AutoPhoto.Services;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using WindowsInput.Native;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Media;
using AutoPhoto.Utils;
using AForge.Imaging;
using System.Linq;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace AutoPhoto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        
        private const string _potion = "Potion";
        private const string _hpPotion = "HP_Potion";
        private string _r2ProccessName = "R2Client";
        private CancellationTokenSource _cancellationToken;
        private InputSimulator _inputSimulator = new InputSimulator();
        private bool _tpPressed = false;
        private Random _rnd = new Random();
        private bool _isPotionWorking = false;
        private bool _isTeleportWorking = false;

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            try
            {
                var startingData = FileService.ReadFromFile();
                if (startingData != null)
                {
                    PotionPixelX.Text = startingData.PotionCountX;
                    PotionPixelY.Text = startingData.PotionCountY;
                    HPPotionPixelX.Text = startingData.HPPotionPixelX;
                    HPPotionPixelY.Text = startingData.HPPotionPixelY;
                    PotionDelay.Text = startingData.PotionDelay;
                    TeleportPixelX.Text = startingData.TeleportPixelX;
                    TeleportPixelY.Text = startingData.TeleportPixelY;
                    TeleportDelay.Text = startingData.TeleportDelay;
                    SwitchWindowCheckBox.IsChecked = startingData.IsSwitchToR2;
                    DuplicateSoundsCheckBox.IsChecked = startingData.IsDuplicateSounds;
                    GamePathTextBox.Text = startingData.GamePath;
                    GameProccessTextBox.Text = startingData.GameProccess;

                    var counter = 0;
                    foreach (var item in TeleportButtonComboBox.Items)
                    {
                        if ((item as TextBlock).Text == startingData.TeleportButton)
                            TeleportButtonComboBox.SelectedIndex = counter;
                        counter++;
                    }

                    ParalyzeCheckBox.IsChecked = startingData.IsCheckParalyze;
                }
            } catch { }
            
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                PotionAndTeleportCancel_Click(sender, e);
        }

        private async void PotionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var delay = Int32.Parse(PotionDelay.Text);
                _r2ProccessName = GameProccessTextBox.Text;

                if (_cancellationToken == null || _cancellationToken.IsCancellationRequested)
                    _cancellationToken = new CancellationTokenSource();

                FileService.SaveToFile(CreateDataForFile());

                PotionButton.IsEnabled = false;
                TeleportButton.IsEnabled = false;
                PotionAndTeleportButton.IsEnabled = false;

                if (_isTeleportWorking)
                {
                    ExceptionTextBlock.Text = "Банки пьются и тпшка жмётся";
                }
                else
                {
                    ExceptionTextBlock.Text = "Банки пьются";
                }

                var points = new Dictionary<string, Point>
                {
                    { _potion, new Point(Int32.Parse(PotionPixelX.Text), Int32.Parse(PotionPixelY.Text)) },
                    { _hpPotion, new Point(Int32.Parse(HPPotionPixelX.Text), Int32.Parse(HPPotionPixelY.Text)) }
                };

                Task.Run(() => StartCheckingForPotion(delay, VirtualKeyCode.VK_Q, points)); //ushort key 0x10
                _isPotionWorking = true;
            }
            catch (Exception ex)
            {
                ExceptionTextBlock.Text = ex.Message;
            }

        }

        private async void TeleportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int teleportPixelX = Int32.Parse(TeleportPixelX.Text);
                int teleportPixelY = Int32.Parse(TeleportPixelY.Text);
                Point point = new Point(teleportPixelX, teleportPixelY);
                var delay = Int32.Parse(TeleportDelay.Text);
                GraphicService.R2Path = GamePathTextBox.Text;
                _r2ProccessName = GameProccessTextBox.Text;

                if (_cancellationToken == null || _cancellationToken.IsCancellationRequested)
                    _cancellationToken = new CancellationTokenSource();

                FileService.SaveToFile(CreateDataForFile());

                PotionButton.IsEnabled = false;
                TeleportButton.IsEnabled = false;
                PotionAndTeleportButton.IsEnabled = false;

                if (_isPotionWorking)
                {
                    ExceptionTextBlock.Text = "Банки пьются и тпшка жмётся";
                }
                else
                {
                    ExceptionTextBlock.Text = "Тпшка жмётся";
                }

                var isBG = TeleportBGCheckBox.IsChecked.HasValue && TeleportBGCheckBox.IsChecked.Value;
                var isSwitchWindow = SwitchWindowCheckBox.IsChecked.HasValue && SwitchWindowCheckBox.IsChecked.Value;
                var teleportButton = (TeleportButtonComboBox.SelectedItem as TextBlock).Text;

                Task.Run(() => StartCheckingForTeleport(point, delay, teleportButton.ToVirtualKeyCode(), isSwitchWindow, isBG)); //ushort key 0x21
                _isTeleportWorking = true;
            }
            catch (Exception ex)
            {
                ExceptionTextBlock.Text = ex.Message;
            }
        }

        private async Task StartCheckingForPotion(int delay, VirtualKeyCode keyCode, Dictionary<string, Point> points)
        {
            do
            {
                Thread.Sleep(_rnd.Next(delay - 25, delay + 25));

                try
                {

                    var colors = GraphicService.GetPixelsFromApplication(_r2ProccessName, points);

                    if (colors[_hpPotion].R < 100 && colors[_hpPotion].R != 0 && colors[_potion].R > 100)
                    {
                        var r2Ptr = GraphicService.GetProccessPointer(_r2ProccessName);
                        var foregroundPtr = GraphicService.GetForegroundWindow();

                        if (r2Ptr != foregroundPtr)
                        {
                            continue;
                        }

                        _inputSimulator.Keyboard.KeyPress(keyCode);
                    }
                }
                catch (Exception ex)
                {
                }
            } while (!_cancellationToken.IsCancellationRequested);
        }

        private async Task StartCheckingForTeleport(Point point, int delay, VirtualKeyCode keyCode, bool switchWindow, bool isBG)
        {
            do
            {
                Thread.Sleep(delay);

                try
                {
                    var color = GraphicService.GetPixelFromApplication(_r2ProccessName, point);

                    if (_tpPressed)
                    {
                        if (color.R > 100)
                        {
                            _tpPressed = false;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (color.R < 100 && color.R != 0)
                    {
                        var r2Ptr = GraphicService.GetProccessPointer(_r2ProccessName);
                        var foregroundPtr = GraphicService.GetForegroundWindow();
                        if (switchWindow && r2Ptr != foregroundPtr)
                        {
                            GraphicService.SetForegroundWindow(r2Ptr);
                        }
                        else if (!switchWindow && r2Ptr != foregroundPtr)
                        {
                            continue;
                        }

                        if (isBG)
                        {
                            PressButtonForBG(keyCode);
                            continue;
                        }

                        var colorAfterTP = new Color();
                        var tpCounter = 0;
                        do
                        {
                            _inputSimulator.Keyboard.KeyPress(keyCode);
                            Thread.Sleep(_rnd.Next(80, 120));
                            colorAfterTP = GraphicService.GetPixelFromApplication(_r2ProccessName, point);
                            tpCounter++;
                        } while (colorAfterTP.R != 0 && tpCounter < 3);
                        _tpPressed = true;

                        //PressButton(keyCode);
                    }
                }
                catch (Exception ex)
                {
                }
            } while (!_cancellationToken.IsCancellationRequested);
        }

        private void PotionAndTeleportButton_Click(object sender, RoutedEventArgs e)
        {
            PotionAndTeleportButton.IsEnabled = false;
            PotionButton_Click(sender, e);
            TeleportButton_Click(sender, e);
            if (DuplicateSoundsCheckBox.IsChecked.HasValue && DuplicateSoundsCheckBox.IsChecked.Value)
            {
                DuplicateSouns(sender, e);
            }
            if (ParalyzeCheckBox.IsChecked.HasValue && ParalyzeCheckBox.IsChecked.Value)
            {
                CheckParalyze(sender, e);
            }
        }

        private void CheckParalyze(object sender, RoutedEventArgs e)
        {
            Task.Run(() => 
            {
                do
                {
                    try
                    {
                        var sourceBitmap = GraphicService.CaptureApplication(_r2ProccessName).ConvertToFormatAndCut(PixelFormat.Format24bppRgb);
                        Bitmap template = Bitmap.FromFile(@"paralyze.png").ConvertToFormat(PixelFormat.Format24bppRgb);

                        ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.921f);
                        TemplateMatch[] matchings = tm.ProcessImage(sourceBitmap, template);
                        if (matchings.Any())
                        {
                            SoundPlayer player = new SoundPlayer("alarm.wav");
                            player.Play();
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(2000);
                    }
                    catch (Exception)
                    { }
                } while (!_cancellationToken.IsCancellationRequested);
            });
        }

        private void DuplicateSouns(object sender, RoutedEventArgs e)
        {
            var filePath = GamePathTextBox.Text;
            Task.Run(() =>
            {
                do
                {
                    try
                    {
                        FileInfo oFileInfoEF_1182 = new FileInfo(filePath + "\\sound\\effect\\EF_1182.wav");
                        if (oFileInfoEF_1182.LastAccessTime > DateTime.Now.AddSeconds(-5))
                        {
                            SoundPlayer player = new SoundPlayer("sound\\effect\\dts.wav");
                            player.Play();
                            Thread.Sleep(1000);
                        }
                        Thread.Sleep(200);
                    }
                    catch (Exception)
                    {}
                } while (!_cancellationToken.IsCancellationRequested);
            });
        }

        private void PotionAndTeleportCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationToken != null)
            {
                _cancellationToken.Cancel();

                PotionButton.IsEnabled = true;
                TeleportButton.IsEnabled = true;
                PotionAndTeleportButton.IsEnabled = true;
                ExceptionTextBlock.Text = "Отмена";
                _isPotionWorking = false;
                _isTeleportWorking = false;
            }
            else
            {
                ExceptionTextBlock.Text = "Нечего отменять";
            }
        }

        private void PressButton(ushort keyCode)
        {
            Input[] inputs = new Input[]
            {
                new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = keyCode,
                            dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                },
                new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = keyCode,
                            dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }


        private void PressButtonForBG(VirtualKeyCode keyCode)
        {
            _inputSimulator.Mouse.LeftButtonDown();
            Thread.Sleep(_rnd.Next(20, 40));
            _inputSimulator.Mouse.LeftButtonUp();
            Thread.Sleep(_rnd.Next(20, 40));
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_S);
            Thread.Sleep(_rnd.Next(20, 40));
            _inputSimulator.Keyboard.KeyPress(keyCode);
            Thread.Sleep(_rnd.Next(20, 40));
            _inputSimulator.Keyboard.KeyPress(keyCode);
            Thread.Sleep(_rnd.Next(20, 40));
            
        }

        private DataForFile CreateDataForFile()
        {
            var result = new DataForFile
            {
                HPPotionPixelX = HPPotionPixelX.Text,
                HPPotionPixelY = HPPotionPixelY.Text,
                PotionDelay = PotionDelay.Text,
                TeleportPixelX = TeleportPixelX.Text,
                TeleportPixelY = TeleportPixelY.Text,
                TeleportDelay = TeleportDelay.Text,
                PotionCountX = PotionPixelX.Text,
                PotionCountY = PotionPixelY.Text,
                IsSwitchToR2 = SwitchWindowCheckBox.IsChecked ?? false,
                IsDuplicateSounds = DuplicateSoundsCheckBox.IsChecked ?? false,
                GamePath = GamePathTextBox.Text,
                GameProccess = GameProccessTextBox.Text,
                TeleportButton = TeleportButtonComboBox.SelectedItem != null ? (TeleportButtonComboBox.SelectedItem as TextBlock).Text : string.Empty,
                IsCheckParalyze = ParalyzeCheckBox.IsChecked ?? false,
            };
            return result;
        }
    }
}
