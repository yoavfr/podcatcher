﻿using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace PodCatch.WindowsPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EpisodePage : Page
    {
        private NavigationHelper m_NavigationHelper;
        private IServiceContext m_ServiceContext;
        private EpisodePageViewModel m_DefaultViewModel;

        public EpisodePage()
        {
            m_ServiceContext = PhoneServiceContext.Instance;
            this.InitializeComponent();

            this.m_NavigationHelper = new NavigationHelper(this);
            this.m_NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.m_NavigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        public EpisodePageViewModel DefaultViewModel
        {
            get
            {
                if (m_DefaultViewModel == null)
                {
                    var podcastDataSource = m_ServiceContext.GetService<IPodcastDataSource>();
                    m_DefaultViewModel = new EpisodePageViewModel(podcastDataSource, m_ServiceContext);
                }
                return m_DefaultViewModel;
            }
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.m_NavigationHelper; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            m_DefaultViewModel.Load((string)e.NavigationParameter);
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.m_NavigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.m_NavigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void OnEpisodePlay(object sender, RoutedEventArgs e)
        {
            m_DefaultViewModel.TogglePlayState();
        }

        private void OnSkipNextClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnSkipPreviousClicked(object sender, RoutedEventArgs e)
        {

        }

        private void PlayEpisodeSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {

        }

        private void PlayEpisodeSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

        }
    }
}
