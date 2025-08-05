using PrintAndScan4Ukraine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Model
{
    public partial class Package : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public string OriginalPackageId { get; set; } = string.Empty; // Set this when editing starts

        // --- INotifyDataErrorInfo ---
        public bool HasErrors => _errors.Any();
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
            => _errors.TryGetValue(propertyName!, out var errors) ? errors : Enumerable.Empty<string>();

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private async Task ValidateNewPackageIdAsync()
        {
            ClearErrors(nameof(NewPackageId));

            if (string.IsNullOrWhiteSpace(NewPackageId))
            {
                AddError(nameof(NewPackageId), "Package ID cannot be empty.");
                return;
            }

            if (NewPackageId == OriginalPackageId)
                return;

            IPackageDataProvider _packageDataProvider = new APIPackageDataProvider(new ApiService(Secrets.ApiKey));
            if (await _packageDataProvider.VerifyIfExists(NewPackageId))
                AddError(nameof(NewPackageId), "ID already exists.");
        }
    }
}
