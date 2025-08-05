using PrintAndScan4Ukraine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.ViewModel
{
    public partial class PrintViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public IEnumerable GetErrors(string? propertyName)
            => _errors.TryGetValue(propertyName!, out var errors) ? errors : Enumerable.Empty<string>();

        public bool HasErrors => _errors.Any();
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected void OnPropertyChanged([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void AddError(string propertyName, string error)
        {
            _errors[propertyName] = new List<string> { error };
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private async Task ValidateRangeAsync()
        {
            ClearErrors(nameof(Starting));
            ClearErrors(nameof(Ending));

            if (Starting < 100000000 || Starting > 999999999)
            {
                AddError(nameof(Starting), "9 digits please");
                CanPrint = false;
                return;
            }
            if (Ending < 100000000 || Ending > 999999999)
            {
                AddError(nameof(Ending), "9 digits please");
                CanPrint = false;
                return;
            }

            if (await _dataProvider.FindPackagesBetweenRange(Starting, Ending))
            {
                AddError(nameof(Starting), "Packages within these ranges already exist.");
                //AddError(nameof(Ending), "Packages within these ranges already exist.");
                CanPrint = false;
                return;
            }

            CanPrint = true;
        }
    }
}
