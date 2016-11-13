using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace IoTBrowser
{
    public class BrowserViewModel : ViewModelBase
    {
        public BrowserViewModel(SynchronizationContext synchronizationContext) : base(synchronizationContext)
        {

        }

        string _webAddress = "http://www.Bing.Com";
        public string WebAddress
        {
            get { return _webAddress; }
            set
            {
                _webAddress = value;
                OnPropertyChanged(() => WebAddress);
            }
        }

        Visibility _isLoading = Visibility.Collapsed;
        public Visibility IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(() => IsLoading);
            }
        }
    }
}
