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

        
        private const string R2ProccessName = "R2Client";
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
                    PotionCountPixelX.Text = startingData.PotionCountX;
                    PotionCountPixelY.Text = startingData.PotionCountY;
                    PotionPixelX.Text = startingData.PotionPixelX;
                    PotionPixelY.Text = startingData.PotionPixelY;
                    PotionDelay.Text = startingData.PotionDelay;
                    TeleportPixelX.Text = startingData.TeleportPixelX;
                    TeleportPixelY.Text = startingData.TeleportPixelY;
                    TeleportDelay.Text = startingData.TeleportDelay;
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
                
                int potionPixelX = Int32.Parse(PotionPixelX.Text);
                int potionPixelY = Int32.Parse(PotionPixelY.Text);
                int potionCountX = Int32.Parse(PotionCountPixelX.Text);
                int potionCountY = Int32.Parse(PotionCountPixelY.Text);
                Point point = new Point(potionPixelX, potionPixelY);
                Point pointCount = new Point(potionCountX, potionCountY);
                var delay = Int32.Parse(PotionDelay.Text);
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
                var points = new Dictionary<string, Point>();
                points.Add("Potion", new Point(pointCount.X, pointCount.Y));
                points.Add("HP_Potion", new Point(point.X, point.Y));
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

                Task.Run(() => StartCheckingForTeleport(point, delay, VirtualKeyCode.VK_F, isSwitchWindow, isBG)); //ushort key 0x21
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

                    var colors = GraphicService.GetPixelsFromApplication(R2ProccessName, points);

                    if (colors["HP_Potion"].R < 100 && colors["HP_Potion"].R != 0 && colors["Potion"].R > 35)
                    {
                        var r2Ptr = GraphicService.GetProccessPointer(R2ProccessName);
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
                    var color = GraphicService.GetPixelFromApplication(R2ProccessName, point);

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
                        var r2Ptr = GraphicService.GetProccessPointer(R2ProccessName);
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

                        _inputSimulator.Keyboard.KeyPress(keyCode);
                        Thread.Sleep(_rnd.Next(20, 40));
                        _inputSimulator.Keyboard.KeyPress(keyCode);
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
                PotionPixelX = PotionPixelX.Text,
                PotionPixelY = PotionPixelY.Text,
                PotionDelay = PotionDelay.Text,
                TeleportPixelX = TeleportPixelX.Text,
                TeleportPixelY = TeleportPixelY.Text,
                TeleportDelay = TeleportDelay.Text,
                PotionCountX = PotionCountPixelX.Text,
                PotionCountY = PotionCountPixelY.Text
            };
            return result;
        }
    }
}
