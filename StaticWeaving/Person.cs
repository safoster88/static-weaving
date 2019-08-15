using StaticWeaving.Common;
using System.ComponentModel;

namespace StaticWeaving
{
    [NotifyPropertyChanged]
    public class Person
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
