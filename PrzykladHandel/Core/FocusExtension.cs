using System;
using System.Windows;
using System.Windows.Threading;

namespace PrzykladHandel.Core
{
    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusExtension), new FrameworkPropertyMetadata(IsFocusedChanged) { BindsTwoWayByDefault = true });

        private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            FrameworkElement grameworkElement = (FrameworkElement)d;

            if (args.OldValue == null)
            {
                grameworkElement.GotFocus += FrameworkElement_GotFocus;
                grameworkElement.LostFocus += FrameworkElement_LostFocus;
            }

            if (!grameworkElement.IsVisible)
            {
                grameworkElement.IsVisibleChanged += new DependencyPropertyChangedEventHandler(FrameworkElement_IsVisibleChanged);
            }

            if (args.NewValue != null && (bool)args.NewValue)
            {
                _ = grameworkElement.Dispatcher.BeginInvoke(new Action(() => { _ = grameworkElement.Focus(); }), DispatcherPriority.Loaded);
            }
        }

        private static void FrameworkElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement)sender;
            if (fe.IsVisible && (bool)fe.GetValue(IsFocusedProperty))
            {
                fe.IsVisibleChanged -= FrameworkElement_IsVisibleChanged;
                _ = fe.Dispatcher.BeginInvoke(new Action(() => { _ = fe.Focus(); }), DispatcherPriority.Loaded);
            }
        }

        private static void FrameworkElement_GotFocus(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).SetValue(IsFocusedProperty, true);
        }

        private static void FrameworkElement_LostFocus(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).SetValue(IsFocusedProperty, false);
        }
    }
}
