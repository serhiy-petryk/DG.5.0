using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGCore.Models
{
    public class FilterOperandModel
    {
        public enum FilterOperTest
        {
            None = 0,
            Contains = 1,
            NotContains = 2,
            Equal = 3,
            NotEqual = 4,
            Between = 5,
            NotBetween = 6,
            Less = 7,
            NotLess = 8,
            Greater = 9,
            NotGreater = 10,
            StartsWith = 11,
            NotStartsWith = 12,
            EndsWith = 13,
            NotEndsWith = 14,
            CanBeNull = 15
        }

        public int Id { get; set; }
        public FilterOperTest Id2 { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertiesChanged("Name");
            }
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
