﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

using Visifire.Charts;

namespace EventHandling
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // Create a Visifire Chart
            CreateChart();
        }

        /// <summary>
        /// Function to create a chart
        /// </summary>
        public void CreateChart()
        {
            // Create a new instance of Chart
            chart = new Chart();

            // Create a new instance of Title
            Title title = new Title();

            // Set title property
            title.Text = "Visifire Sample with Events";

            // Attach event to Title
            title.MouseLeftButtonDown += new MouseButtonEventHandler(title_MouseLeftButtonDown);

            // Add title to Titles collection
            chart.Titles.Add(title);

            // Create a new instance of DataSeries
            DataSeries dataSeries = new DataSeries();

            // Set DataSeries property
            dataSeries.RenderAs = RenderAs.Column;

            // Create a DataPoint
            DataPoint dataPoint;

            for (int i = 0; i < 5; i++)
            {
                // Create a new instance of DataPoint
                dataPoint = new DataPoint();

                // Set YValue for a DataPoint
                dataPoint.YValue = rand.Next(10, 100);

                // Attach event to DataPoint
                dataPoint.MouseLeftButtonUp += new MouseButtonEventHandler(dataPoint_MouseLeftButtonUp);

                // Add dataPoint to DataPoints collection
                dataSeries.DataPoints.Add(dataPoint);
            }

            // Add dataSeries to Series collection
            chart.Series.Add(dataSeries);

            // Add chart to LayoutRoot
            elementHost1.Child = chart;
        }

        /// <summary>
        /// Event handler for MouseLeftButtonDown event
        /// </summary>
        /// <param name="sender">Title</param>
        /// <param name="e">MouseButtonEventArgs</param>
        void title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as Title).FontColor = new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// Event handler for MouseLeftButtonUp event
        /// </summary>
        /// <param name="sender">DataPoint</param>
        /// <param name="e">MouseButtonEventArgs</param>
        void dataPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Update YValue property of the DataPoint
            (sender as DataPoint).YValue = rand.Next(10, 100);
        }

        Chart chart;                                            // Chart object
        Random rand = new Random(DateTime.Now.Millisecond);     // Create a random class variable
    }
}
