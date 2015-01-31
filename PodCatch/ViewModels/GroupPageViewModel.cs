using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;

namespace PodCatch.ViewModels
{
    public class GroupPageViewModel : BaseViewModel<PodcastGroup>
    {
        private IPodcastDataSource m_PodcastDataSource;
        private bool m_ShowingPopUp;

        public PodcastGroupViewModel Group { get; set; }

        public ObservableCollection<PodcastSummaryViewModel> Podcasts { get; private set; }

        public GroupPageViewModel(PodcastGroup group, IServiceContext serviceContext)
            : base(group, serviceContext)
        {
        }

        protected override void UpdateFields()
        {
            if (Data != null)
            {
                Group = new PodcastGroupViewModel(Data, ServiceContext);
                Podcasts = Group.Podcasts;
            }
        }

        public void OnLoadState(PodcastGroup group, IPodcastDataSource podcastDatasource)
        {
            Data = group;
            m_PodcastDataSource = podcastDatasource;
            UpdateFields();
        }

        public async Task OnPodcastTapped(PodcastSummaryViewModel selectedPodcast, Point position)
        {
            if (m_ShowingPopUp == true)
            {
                return;
            }
            m_ShowingPopUp = true;
            try
            {
                PopupMenu popupMenu = new PopupMenu();
                // this is useful for debugging
                //popupMenu.Commands.Add(new UICommand(){Id=1, Label="Copy RSS feed URL to clipboard"});

                if (m_PodcastDataSource.IsPodcastInFavorites(selectedPodcast.Data))
                {
                    popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Remove from favorites" });
                }
                else
                {
                    popupMenu.Commands.Add(new UICommand() { Id = 3, Label = "Add to favorites" });
                }
                IUICommand selectedCommand = await popupMenu.ShowAsync(position);
                if (selectedCommand == null)
                {
                    return;
                }
                switch ((int)selectedCommand.Id)
                {
                    case 2: // Remove from favorites
                        await m_PodcastDataSource.RemoveFromFavorites(selectedPodcast.Data);
                        //NavigationHelper.GoBack();
                        break;

                    case 3: // Add to favorites
                        await m_PodcastDataSource.AddToFavorites(selectedPodcast.Data);
                        break;
                }
            }
            finally
            {
                m_ShowingPopUp = false;
            }
        }
    }
}