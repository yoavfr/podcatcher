﻿using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace PodCatch
{
    public sealed partial class InputMessageDialog : UserControl
    {
        private string m_TextMessage;
        private TaskCompletionSource<bool> m_TaskCompletionSource;

        public InputMessageDialog(string label)
        {
            Label = label;
            this.InitializeComponent();
        }

        public Task<bool> ShowAsync()
        {
            Init();
            m_Popup.IsOpen = true;
            m_TaskCompletionSource = new TaskCompletionSource<bool>();
            m_TextBox.Focus(FocusState.Programmatic);
            return m_TaskCompletionSource.Task;
        }

        public void Init()
        {
            m_Rect1.Height = Window.Current.Bounds.Height;
            m_Rect1.Width = Window.Current.Bounds.Width;
            m_Rect2.Width = Window.Current.Bounds.Width;
            m_TextBox.Width = Window.Current.Bounds.Width / 2;
            m_TextBlock.Text = Label;
        }

        public string Label
        {
            get { return m_TextMessage; }
            set { m_TextMessage = value; }
        }

        public TextBox TextBox
        {
            get { return m_TextBox; }
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            m_TaskCompletionSource.SetResult(true);
            m_Popup.IsOpen = false;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            m_TaskCompletionSource.SetResult(false);
            m_Popup.IsOpen = false;
        }

        private void OnKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                OkClicked(sender, e);
            }
            else if (e.Key == VirtualKey.Escape)
            {
                CancelClicked(sender, e);
            }
        }
    }
}