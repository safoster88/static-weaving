using System;
using System.Collections.ObjectModel;
using System.Text;

namespace StaticWeaving
{
    public class MainViewModel
    {
        public ObservableCollection<Person> People { get; set; }

        public Command ChangeContent { get; set; }

        private readonly Random _random = new Random();

        public MainViewModel()
        {
            People = new ObservableCollection<Person>(new Person[]
            {
                new Person { FirstName = "Stephen", LastName = "Foster" }
            });

            ChangeContent = new Command(() =>
            {
                foreach (var person in People)
                {
                    person.FirstName = GetRandomString();
                    person.LastName = GetRandomString();
                }
            });
        }

        private string GetRandomString(int minLength = 3, int maxLength = 10)
        {
            var capacity = _random.Next(minLength, maxLength);

            var sb = new StringBuilder(capacity);

            for (var i = 0; i < capacity; i++)
            {
                sb.Append((char)_random.Next(97, 122 + 1));
            }

            return sb.ToString();
        }
    }
}
