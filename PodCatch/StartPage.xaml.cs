﻿using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace PodCatch
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public StartPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
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
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var podcastDataGroups = await PodcastDataSource.LoadGroupsFromCacheAsync();
            this.DefaultViewModel["Groups"] = podcastDataGroups;
            try
            {
                podcastDataGroups = await PodcastDataSource.LoadGroupsFromRssAsync();
            }
            catch (Exception ex)
            {
                // TODO: trace or alert
            }
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            this.Frame.Navigate(typeof(GroupPage), ((PodcastGroup)group).UniqueId);
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((Podcast)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(PodcatchPath), itemId);
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void AddToFavoritesButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            AddToFavoritesAppBarButton.Flyout.Hide();
            Podcast newItem = new Podcast(string.Empty, RssUrl.Text, string.Empty, string.Empty, null);
            PodcastDataSource.AddItem("Favorites", newItem);
            try
            {
                await newItem.LoadFromRssAsync();
            }
            catch (Exception ex)
            {
                PodcastDataSource.RemoveItem("Favorites", newItem);
                return;
            }
            try
            {
                PodcastDataSource.Store();
            }
            catch (Exception ex)
            {

            }
        }

        private void RemoveFromFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            /*BottomAppBar.IsOpen = false;
            PodcastDataItem selectedItem = m_RightClickedPodcast;
*/
        }

        private async void PodcastRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            PopupMenu popupMenu = new PopupMenu();
            popupMenu.Commands.Add(new UICommand(){Id=1, Label="Copy RSS feed to clipboard"});
            popupMenu.Commands.Add(new UICommand() {Id = 2, Label = "Remove from favorites"});
            //GeneralTransform pointTransform = ((GridView)sender).TransformToVisual(Window.Current.Content);
            //Point screenCoords = pointTransform.TransformPoint(new Point(50, 10));
            Grid grid = (Grid)sender;
            IUICommand selectedCommand = await popupMenu.ShowAsync(e.GetPosition(this));
            if (selectedCommand == null)
            {
                return;
            }
            Podcast selectedItem = (Podcast)grid.DataContext;
            switch ((int)selectedCommand.Id)
            {
                case 1:
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(selectedItem.Uri);
                    Clipboard.SetContent(dataPackage);
                    break;
                case 2:
                    PodcastDataSource.RemoveItem("Favorites", selectedItem);
                    PodcastDataSource.Store();
                    NavigationHelper.GoBack();
                    break;
            }

        }
        
    }
}