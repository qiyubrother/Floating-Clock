﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace FloatingClock
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private NotifyIcon notifyIcon;
        private DispatcherTimer refreshDispatcher;
        public static MainWindow current;
        public static bool windowIsVisible;
        public static bool HotCornerEnabled;

        /// <summary>
        /// Initialize Application and Main Window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            current = this;
            Refresh();
            ShowClock();
            InitializeRefreshDispatcher();
            WaitToFullMinuteAndRefresh();
            new HotKey(Key.C, KeyModifier.Alt, key => ShowClock());
            EnableHotCorner(true);

            TrayIcon();


        }

        private void EnableHotCorner(bool enable)
        {
            if (enable)
                MouseHook._hookID = MouseHook.SetHook(MouseHook._proc);
            else MouseHook.UnhookWindowsHookEx(MouseHook._hookID);
            HotCornerEnabled = enable;
        }

        /// <summary>
        /// Prepare Clock to Show 
        /// </summary>
        public void ShowClock()
        {
            SetPositionOnCurrentDisplay();
            Refresh();
            InitializeAnimationIn();
            WaitToFullMinuteAndRefresh();
        }

        /// <summary>
        /// Load Current Data to Controls
        /// </summary>
        private void LoadCurrentClockData()
        {
            var timeNow = DateTime.Now;
            Hours.Text = timeNow.ToString("HH");
            Minutes.Text = timeNow.ToString("mm");
            DayOfTheWeek.Text = timeNow.ToString("dddd");
            DayOfTheMonth.Text = timeNow.ToString("dd") + " " + timeNow.ToString("MMMM");
        }

        /// <summary>
        /// Initialize Refresh Dispatcher
        /// </summary>
        private void InitializeRefreshDispatcher()
        {
            refreshDispatcher = new DispatcherTimer();
            refreshDispatcher.Tick += Refresh;
            refreshDispatcher.Interval = new TimeSpan(0, 1, 0);
        }

        /// <summary>
        /// Wait to full minute refresh data and start refresh Dispatcher
        /// </summary>
        private async void WaitToFullMinuteAndRefresh()
        {
            await Task.Delay((60 - DateTime.Now.Second) * 1000);
            Refresh();
            refreshDispatcher.Start();
        }

        /// <summary>
        /// DispatcherTimer Refresh Event
        /// </summary>
        /// <param name="sender">Dispatcher</param>
        /// <param name="e">Dispatcher Arg</param>
        private void Refresh(object sender = null, EventArgs e = null)
        {
            LoadCurrentClockData();
        }
        /// <summary>
        /// Set position on current Display 
        /// </summary>
        private void SetPositionOnCurrentDisplay()
        {
            var activeScreen = Screen.FromPoint(Control.MousePosition);
            Application.Current.MainWindow.Top = (activeScreen.Bounds.Height + activeScreen.Bounds.Y) - 140 - 48;
            Application.Current.MainWindow.Left = activeScreen.Bounds.X + 50;
        }

        /// <summary>
        /// Initialize Tray Icon and BaloonTip
        /// </summary>
        private void TrayIcon()
        {
            notifyIcon = new NotifyIcon();
            //     notifyIcon.Click += notifyIcon_Click;
            ContextMenu m_menu;



            m_menu = new ContextMenu();
            var ActiveHotCornerItem = new MenuItem("Activate HotCorner", ChangeHotCornerActiveState);
            ActiveHotCornerItem.Checked = HotCornerEnabled;
            var OptionsItem = new MenuItem("Options", OpenOptionWindow);
            OptionsItem.Enabled = false;
            var ExitItem = new MenuItem("Exit", CloseWindow);
            m_menu.MenuItems.Add(0, (ActiveHotCornerItem));
            m_menu.MenuItems.Add(1, (OptionsItem));
            m_menu.MenuItems.Add(2, (ExitItem));
            notifyIcon.ContextMenu = m_menu;


            var streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/clock.ico"));
            if (streamResourceInfo != null)
                notifyIcon.Icon = new Icon(streamResourceInfo.Stream);


            notifyIcon.Visible = true;



            notifyIcon.ShowBalloonTip(5, "Hello " + Environment.UserName,
                "Press Alt+C to show Clock\nRight Click on Tray to Close", ToolTipIcon.Info);
        }

        private void OpenOptionWindow(object sender, EventArgs e)
        {

        }

        private void ChangeHotCornerActiveState(object sender, EventArgs e)
        {
            EnableHotCorner(!HotCornerEnabled);
            (sender as MenuItem).Checked =HotCornerEnabled;
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Closing app after Right Click
        /// </summary>
        /// <param name="sender">NotifyIcon Click Event</param>
        /// <param name="e">MouseEventArg (Left Right Mouse button)</param>
        private void notifyIcon_Click(object sender, EventArgs e)
        {
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs != null && mouseEventArgs.Button == MouseButtons.Right)
                Close();
        }

        /// <summary>
        /// Start Animation FadeIN
        /// </summary>
        private void InitializeAnimationIn()
        {
            Application.Current.MainWindow.Activate();
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeIn;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            windowIsVisible = true;
            dispatcherTimer.Start();

        }

        /// <summary>
        /// Animation Fade In Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeIn(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity < 0.95)
                Application.Current.MainWindow.Opacity += 0.05;
            else
                ((DispatcherTimer)sender).Stop();
        }

        /// <summary>
        /// Start Animation FadeOut Event
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeOut;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);

            dispatcherTimer.Start();
            windowIsVisible = false;
            refreshDispatcher.Stop();
        }

        /// <summary>
        /// Animation Fade Out Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeOut(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity > 0)
                Application.Current.MainWindow.Opacity -= 0.1;
            else
            {
                ((DispatcherTimer)sender).Stop();
                Application.Current.MainWindow.Visibility = Visibility.Collapsed;
            }
        }
    }
}