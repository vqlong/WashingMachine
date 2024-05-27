﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XLib.Converters
{
    public class DoubleDivideParameterConverter : IValueConverter
    {
        public static DoubleDivideParameterConverter Default { get; } = new DoubleDivideParameterConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double dividend && double.TryParse(parameter.ToString(), out double divisor)) return dividend / divisor;
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
