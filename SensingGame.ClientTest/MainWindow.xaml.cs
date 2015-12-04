﻿using SensngGame.ClientSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SensingGame.ClientTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GameServiceClient gameSvc = new GameServiceClient("williamwu", "wx37e46819d148d5fb", "1", "3");
        public MainWindow()
        {
            InitializeComponent();

            
            Loaded += MainWindow_Loaded;
            //timer.Interval = TimeSpan.FromSeconds(2);
            //timer.Tick += Timer_Tick;
            //timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(currentQrCode))
            {
                var user = await gameSvc.FindScanQrCodeUserAsync(currentQrCode);
                if (user != null && user.Data != null)
                {
                    avatorImg.Source = new BitmapImage(new Uri(user.Data.HeadImagUrl));

                    await gameSvc.PostDataByUserAsync(user.Data.QrCodeId, @"E:\TFS\SensingGame\SensingGame\bin\Debug\welcome.png", @"E:\TFS\SensingGame\SensingGame\bin\Debug\welcome.png",80);
                }

                var user1 = await gameSvc.FindScanQrCodeUserAsync(currentPDQrCode);
                if (user1 != null && user.Data != null)
                {
                    avatorImg.Source = new BitmapImage(new Uri(user1.Data.HeadImagUrl));
                }

            }
        }

        private string currentQrCode;
        private string currentPDQrCode;

        private DispatcherTimer timer = new DispatcherTimer();
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            var data = await gameSvc.GetQrCode4LoginAsync();
            if (data != null && data.Data != null)
            {
                var qrcode = data.Data;
                currentQrCode = qrcode.QrCodeId;
                image.Source = new BitmapImage(new Uri(qrcode.QrCodeUrl));
            }
            var postBack = await gameSvc.PostData4ScanAsync(@"E:\TFS\SensingGame\SensingGame\bin\Debug\welcome.png", @"E:\TFS\SensingGame\SensingGame\bin\Debug\welcome.png",50);
            if (postBack != null && postBack.Data != null)
            {
                var qrcode = postBack.Data;
                currentPDQrCode = qrcode.QrCodeId;
                image1.Source = new BitmapImage(new Uri(qrcode.QrCodeUrl));
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Timer_Tick(null,null);
        }
    }
}
