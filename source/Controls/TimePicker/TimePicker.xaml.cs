using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace TemperatureMeasurementTool
{
    /// <summary>
    /// Interaktionslogik für TimePicker.xaml
    /// </summary>
    public partial class TimePicker : UserControl
    {
        public TimePicker()
        {
            InitializeComponent();
            Hour.Text = DateTime.Now.ToString("HH");
            Minute.Text = DateTime.Now.ToString("mm");
        }

        /// <summary>
        /// Zählt die Stunden hoch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpHour_OnClick(object sender, RoutedEventArgs e)
        {
            var value = Convert.ToInt32(Hour.Text);
            if (value < 23)
            {
                Hour.Text = (value + 1) <= 9 ? "0" + (++value).ToString() : (++value).ToString();
            }
            else if (value == 23)
            {
                Hour.Text = "01";
            }
        }

        /// <summary>
        /// Zählt die Minuten hoch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpMinute_OnClick(object sender, RoutedEventArgs e)
        {
            var value = Convert.ToInt32(Minute.Text);
            if (value < 59)
            {
                Minute.Text = (value + 1) <= 9 ? "0" + (++value).ToString() : (++value).ToString();
            }
            else if (value == 59)
            {
                Minute.Text = "01";
            }
        }

        /// <summary>
        /// Zählt die Stunden runter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownHour_OnClick(object sender, RoutedEventArgs e)
        {
            var value = Convert.ToInt32(Hour.Text);
            if (value > 0)
            {
                Hour.Text = (value - 1) <= 9 ? "0" + (--value).ToString() : (--value).ToString();
            }
            else if (value == 0)
            {
                Hour.Text = "23";
            }
        }

        /// <summary>
        /// Zählt die Minuten runter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownMinute_OnClick(object sender, RoutedEventArgs e)
        {
            var value = Convert.ToInt32(Minute.Text);
            if (value > 0)
            {
                Minute.Text = (value - 1) <= 9 ? "0" + (--value).ToString() : (--value).ToString();
            }
            else if (value == 0)
            {
                Minute.Text = "59";
            }

        }

        /// <summary>
        /// ToDo: Change return Value to DateTime
        /// </summary>
        /// <returns></returns>
        public int GetValueAsDateTime()
        {
            return 1230;
        }

        /// <summary>
        /// Gibt den Wert zurück
        /// </summary>
        /// <returns></returns>
        public string GetValueAsString()
        {
            return Hour.Text + ":" + Minute.Text;
        }

        /// <summary>
        /// Setzt die Uhrzeit programmatically
        /// </summary>
        public void SetValueAsString(string hour, string minute)
        {
            //TODO Check if hour and Minute is correct Value (Regex)
            Hour.Text = hour;
            Minute.Text = minute;
        }

        /// <summary>
        /// Wenn das Stundenfeld Fokus erlangt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Hour.SelectAll();
        }

        /// <summary>
        /// Wenn das Minutenfeld Fokus erlangt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Minute.SelectAll();
        }

        /// <summary>
        /// Wenn der User die linke MausTaste in dieses Feld rein klickt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Minute.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Minute.Focus();
            }
        }

        /// <summary>
        /// Wenn der User die linke MausTaste in dieses Feld rein klickt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Hour.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Hour.Focus();
            }
        }

        /// <summary>
        /// Wenn der Fokus aus dem Minutenfeld kommt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var resultMinute = Convert.ToInt32(Minute.Text);
            if (resultMinute > 59)
            {
                Minute.Text = "59";
            }
        }

        /// <summary>
        /// Überprüft die EIngabder der Minute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }
        /// <summary>
        /// Überprüft die Eingabe in das Stundenfeld
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        /// <summary>
        /// Wenn der Fokus aus dem Feld verschwindet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var resultHour = Convert.ToInt32(Hour.Text);
            if (resultHour > 23)
            {
                Hour.Text = "23";
            }
        }

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00A8DE"));
        }

        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = Brushes.White;
        }
    }
}