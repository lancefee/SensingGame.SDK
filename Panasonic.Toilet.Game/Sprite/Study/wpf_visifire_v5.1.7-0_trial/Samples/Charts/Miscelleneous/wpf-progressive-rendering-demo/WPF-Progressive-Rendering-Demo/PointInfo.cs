﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace WPF_Progressive_Rendering_Demo
{
    public class PointInfo : INotifyPropertyChanged
    {
       
        public Double YValue
        {
            get
            {
                return _YValue;
            }
            set
            {
                _YValue = value;
                FirePropertyChanged("YValue");
            }
        }

        public String AxisXLabel
        {
            get
            {
                return _AxisXLabel;
            }
            set
            {
                _AxisXLabel = value;
                FirePropertyChanged("AxisXLabel");
            }
        }

        public Double XValue
        {   
            get
            {   
                return _xValue;
            }
            set
            {   
                _xValue = value;
                FirePropertyChanged("XValue");
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        public void FirePropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        Double _xValue;
        String _AxisXLabel;
        Double _YValue;
    }
}
