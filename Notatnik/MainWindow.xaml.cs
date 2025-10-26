﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.VisualStyles;
using System.Linq.Expressions;
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.VisualBasic;



namespace Notatnik
{
    public partial class MainWindow : Window
    {
        private TXTFiles _txtFilesPage;
        private Planer _planerPage;
        private PlanerFormat _planerFormatPage;





        public MainWindow()
        {
            InitializeComponent();
            _txtFilesPage = new TXTFiles();
        }


        private void TXTFiles_bt_click(object sender, RoutedEventArgs e)
        {
            MainWindow1.MinHeight = 570;
            MainWindow1.MinWidth = 950;
            if(MainWindow1.Height < 570 || MainWindow1.Width < 950)
            {
                MainWindow1.Height = 570;
                MainWindow1.Width = 950;
            }

            MainFrame.Navigate(_txtFilesPage);
            
        }

        private void Planer_bt_click(object sender, RoutedEventArgs e)
        {
            MainWindow1.MinHeight = 570;
            MainWindow1.MinWidth = 950;
            if (MainWindow1.Height < 570 || MainWindow1.Width < 950)
            {
                MainWindow1.Height = 570;
                MainWindow1.Width = 950;
            }

            MainFrame.Navigate(_planerPage);
        }

        private void Format_planer_txt_click(object sender, RoutedEventArgs e)
        {
            MainWindow1.MinHeight = 570;
            MainWindow1.MinWidth = 950;
            if (MainWindow1.Height < 570 || MainWindow1.Width < 950)
            {
                MainWindow1.Height = 570;
                MainWindow1.Width = 950;
            }

            MainFrame.Navigate(_planerFormatPage);
        }

        private void reset_window_click(object sender, RoutedEventArgs e)
        {
            MainWindow1.MinHeight = 400;
            MainWindow1.MinWidth = 150;
            MainWindow1.Height = 400;
            MainWindow1.Width = 150;
            MainFrame.Navigate(null);
        }
    }
}


