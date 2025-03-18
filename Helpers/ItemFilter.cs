using System.ComponentModel;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace cFollower
{
    public class ItemFilter : INotifyPropertyChanged
    {
        private bool _enabled;
        private string _name;
        private string _renderitem;

        

        public ItemFilter()
        {
        }

        public ItemFilter(bool enabled, string name, string renderitem)
        {
            _enabled = enabled;
            _name = name;
            _renderitem = renderitem;
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                NotifyPropertyChanged(nameof(Enabled));
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        public string RenderItem
        {
            get { return _renderitem; }
            set
            {
                _renderitem = value;
                NotifyPropertyChanged(nameof(RenderItem));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}