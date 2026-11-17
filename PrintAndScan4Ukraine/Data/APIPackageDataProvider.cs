using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
    public class APIPackageDataProvider(IApiService apiService) : IPackageDataProvider
    {
        private readonly HttpClient _httpClient = apiService.Client;

        public async Task<IEnumerable<MissingPackages>> FindMissingPackages(List<string> barcodesNotInPackages, CancellationToken cancellationToken = default)
        {
            var requestBody = new { barcodes = barcodesNotInPackages };

            var response = await _httpClient.PostAsJsonAsync("/find-missing-packages", requestBody, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get missing packages: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to get missing packages: {response.StatusCode} - {error}");
                return new List<MissingPackages>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<MissingPackages>>(json) ?? new List<MissingPackages>();
        }

        public async Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad, CancellationToken cancellationToken = default)
        {
            Libmiroppb.Log($"Get List of Packages{(initialLoad ? "" : " for a refresh")}");
            IEnumerable<Package> packages = [];
            try
            {
                var response = await _httpClient.GetAsync("/packages", cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Failed to get packages: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}1:\n{error}");
                    return packages;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<List<Package>>(json);
                if (result != null)
                {
                    packages = [.. result.Select(x =>
                    {
                        x.Recipient_Contents = !string.IsNullOrEmpty(x.Contents)
                            ? JsonConvert.DeserializeObject<ObservableCollection<Contents>>(x.Contents)!
                            : new ObservableCollection<Contents>();
                        return x;
                    })];
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "There is no connection to: {Secrets.ApiBaseUrl}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}1:\n{ex.Message}");
            }

            return packages;
        }

        public async Task<PackageSyncPage> GetPackageChangesAsync(string? since, string? cursor, int limit = 500, CancellationToken cancellationToken = default)
        {
            var url = $"/packages/sync?limit={limit}";
            if (!string.IsNullOrWhiteSpace(since))
                url += $"&since={Uri.EscapeDataString(since)}";
            if (!string.IsNullOrWhiteSpace(cursor))
                url += $"&cursor={Uri.EscapeDataString(cursor)}";

            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get package changes: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                return new PackageSyncPage();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var page = JsonConvert.DeserializeObject<PackageSyncPage>(json) ?? new PackageSyncPage();

            foreach (var pkg in page.Packages)
            {
                if (!string.IsNullOrWhiteSpace(pkg.Contents))
                {
                    try
                    {
                        pkg.Recipient_Contents = JsonConvert.DeserializeObject<ObservableCollection<Contents>>(pkg.Contents) ?? new ObservableCollection<Contents>();
                    }
                    catch
                    {
                        pkg.Recipient_Contents = new ObservableCollection<Contents>();
                    }
                }
                else
                {
                    pkg.Recipient_Contents = new ObservableCollection<Contents>();
                }
            }

            return page;
        }

        public void ApplyPackageChanges(ObservableCollection<Package> packages, Package? currentlySelected, IEnumerable<Package> changed)
        {
            if (changed == null) return;

            foreach (var package in changed)
            {
                if (package.Recipient_Contents == null)
                {
                    if (!string.IsNullOrWhiteSpace(package.Contents))
                    {
                        try { package.Recipient_Contents = JsonConvert.DeserializeObject<ObservableCollection<Contents>>(package.Contents) ?? new ObservableCollection<Contents>(); }
                        catch { package.Recipient_Contents = new ObservableCollection<Contents>(); }
                    }
                    else
                    {
                        package.Recipient_Contents = new ObservableCollection<Contents>();
                    }
                }

                if (package.Removed)
                {
                    var existing = packages.FirstOrDefault(x => x.Id == package.Id || x.PackageId == package.PackageId);
                    if (existing != null && (currentlySelected == null || existing.Id != currentlySelected.Id))
                    {
                        packages.Remove(existing);
                    }
                    continue;
                }

                var fromPrevious = packages.FirstOrDefault(x => x.Id == package.Id || x.PackageId == package.PackageId);
                if (fromPrevious != null)
                {
                    if (currentlySelected != null &&
                        (fromPrevious.Id == currentlySelected.Id || fromPrevious.PackageId == currentlySelected.PackageId))
                    {
                        continue;
                    }

                    var (same, variances) = ComparePackages(fromPrevious, package);
                    if (!same)
                    {
                        ReplaceProperties(fromPrevious, variances);
                    }
                }
                else
                {
                    packages.Add(package);
                }
            }
        }

        public async Task<IEnumerable<Package_Status>?> GetAllStatuses(List<string> ids, bool useArchive = false, CancellationToken cancellationToken = default)
        {
            Libmiroppb.Log("Get List of Packages Statuses");

            IEnumerable<Package_Status>? statuses = [];

            try
            {
                var requestBody = new { ids, use_archive = useArchive };

                var response = await _httpClient.PostAsJsonAsync("/package-statuses", requestBody, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    statuses = JsonConvert.DeserializeObject<List<Package_Status>>(json);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Server returned error: {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in GetAllStatuses");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}2:\n{ex.Message}");
            }

            return statuses;
        }

        public async Task<IEnumerable<Package>?> GetByNameAsync(string senderName, bool senderOnly = true, bool useArchive = false, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                sender_name = senderName,
                sender_only = senderOnly,
                use_archive = useArchive
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/packages-by-name", payload, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Failed to get packages by name: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                    MessageBox.Show($"API call failed: {response.StatusCode} - {error}");
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? [];

                foreach (var pkg in packages)
                {
                    if (!string.IsNullOrWhiteSpace(pkg.Contents))
                    {
                        try
                        {
                            pkg.Recipient_Contents = JsonConvert.DeserializeObject<ObservableCollection<Contents>>(pkg.Contents) ?? [];
                        }
                        catch
                        {
                            pkg.Recipient_Contents = [];
                        }
                    }
                    else
                    {
                        pkg.Recipient_Contents = [];
                    }
                }

                return packages;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in GetByNameAsync");
                MessageBox.Show($"{Loc.Tr("PAS4U.SearchSelectionWindow.Error", "Error occured")}3:\n{ex.Message}");
                return [];
            }
        }

        public async Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                packageid,
                use_archive = useArchive
            };

            var response = await _httpClient.PostAsJsonAsync("/package-by-id", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get package: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to get package: {response.StatusCode} - {error}");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? new();

            foreach (var pkg in packages)
            {
                if (!string.IsNullOrWhiteSpace(pkg.Contents))
                {
                    try
                    {
                        pkg.Recipient_Contents = JsonConvert.DeserializeObject<ObservableCollection<Contents>>(pkg.Contents) ?? new ObservableCollection<Contents>();
                    }
                    catch
                    {
                        pkg.Recipient_Contents = new ObservableCollection<Contents>();
                    }
                }
                else
                {
                    pkg.Recipient_Contents = new ObservableCollection<Contents>();
                }
            }

            return packages;
        }

        public async Task<IEnumerable<Package>> GetPackagesAsync(List<string> package_ids, bool useArchive = false, CancellationToken cancellationToken = default)
        {
            if (package_ids == null || package_ids.Count == 0)
                return [];

            var payload = new
            {
                package_ids,
                use_archive = useArchive
            };

            var response = await _httpClient.PostAsJsonAsync("/packages-batch", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get packages batch: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to get packages: {response.StatusCode} - {error}");
                return [];
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var packages = JsonConvert.DeserializeObject<List<Package>>(responseContent);
            if (packages == null)
                return [];

            foreach (var pkg in packages)
            {
                if (!string.IsNullOrWhiteSpace(pkg.Contents))
                {
                    try
                    {
                        pkg.Recipient_Contents = JsonConvert.DeserializeObject<ObservableCollection<Contents>>(pkg.Contents) ?? new ObservableCollection<Contents>();
                    }
                    catch
                    {
                        pkg.Recipient_Contents = new ObservableCollection<Contents>();
                    }
                }
                else
                {
                    pkg.Recipient_Contents = new ObservableCollection<Contents>();
                }
            }
            return packages;
        }

        public async Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start_date, DateTime end_date, int status_code, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                start_date,
                end_date,
                status_code
            };

            var response = await _httpClient.PostAsJsonAsync("/packages-by-date-and-status", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Error retrieving packages: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? [];
            return packages;
        }

        public async Task<DateTime> GetServerDate(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/server-date", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get server date: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to get server date: {response.StatusCode} - {error}");
                return DateTime.Now;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (result != null && result.TryGetValue("server_time", out var serverTime))
                return DateTime.Parse(serverTime);
            else
                return DateTime.Now;
        }

        public async Task<IEnumerable<Package_Status>?> GetStatusByPackage(string packageid, CancellationToken cancellationToken = default)
        {
            Libmiroppb.Log($"Get List of Package Statuses for {packageid}");
            IEnumerable<Package_Status>? statuses = [];
            try
            {
                var response = await _httpClient.GetAsync($"/package-status?packageid={Uri.EscapeDataString(packageid)}", cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    statuses = JsonConvert.DeserializeObject<List<Package_Status>>(json);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Server returned error: {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in GetStatusByPackage");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
            }
            return statuses;
        }

        public async Task<IEnumerable<Users>> GetUserIDsAndNames(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/user-ids-and-names", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Error retrieving user IDs and names: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var users = JsonConvert.DeserializeObject<List<Users>>(json) ?? [];
            return users;
        }

        public async Task<bool> InsertRecord(Package package, CancellationToken cancellationToken = default)
        {
            Libmiroppb.Log($"Inserting into Database: {JsonConvert.SerializeObject(package)}");

            try
            {
                var requestBody = new
                {
                    packageid = package.PackageId,
                    sender_name = package.Sender_Name,
                    sender_address = package.Sender_Address,
                    sender_phone = package.Sender_Phone,
                    recipient_name = package.Recipient_Name,
                    recipient_address = package.Recipient_Address,
                    recipient_phone = package.Recipient_Phone,
                    weight = package.Weight,
                    value = package.Value,
                    contents = package.Contents,
                    delivery = package.Delivery,
                    removed = false
                };

                var response = await _httpClient.PostAsJsonAsync("/insert-package", requestBody, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Failed to insert package: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Failed to insert into database");
                return false;
            }
        }

        public async Task<bool> InsertRecordStatus(List<Package_Status> statuses, CancellationToken cancellationToken = default)
        {
            var payload = statuses.Select(p => new
            {
                id = p.Id,
                packageid = p.PackageId,
                createdbyuser = p.Createdbyuser,
                createddate = p.CreatedDate,
                status = p.Status
            });

            var response = await _httpClient.PostAsJsonAsync("/insert-package-statuses", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to insert statuses: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                return false;
            }

            return true;
        }

        public List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses)
        {
            List<Package_less> list = [];
            foreach (Package package in packages)
            {
                Package temp = package.CreateCopy();
                ObservableCollection<Contents> t = package.Recipient_Contents.CreateCopy();
                List<string> output = [];
                foreach (var item in t) { output.Add($"{item.Name}: {item.Amount}"); }

                var jsonParent = JsonConvert.SerializeObject(temp);
                Package_less c = JsonConvert.DeserializeObject<Package_less>(jsonParent)!;
                c.Contents = output.Join(Environment.NewLine);

                List<Package_Status> s = [.. statuses.Where(s => s.PackageId == c.PackageId)];
                List<string> so = [];
                s.ForEach(x => so.Add(x.ToString()));
                c.Statuses = so.Join(Environment.NewLine);

                list.Add(c);
            }
            return list;
        }

        public async Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected, CancellationToken cancellationToken = default)
        {
            if (CurrentlySelected == null) return false;
            IEnumerable<Package>? RefreshedPackages = await GetAllAsync(false, cancellationToken).ConfigureAwait(false);
            if (RefreshedPackages != null)
            {
                foreach (Package package in RefreshedPackages.ToList())
                {
                    Package? FromPrevious = packages.FirstOrDefault(x => x.Id == package.Id);
                    if (FromPrevious != null)
                    {
                        (bool, List<Variance>) Ret = ComparePackages(FromPrevious, package);
                        if (!Ret.Item1)
                        {
                            if (FromPrevious == CurrentlySelected ||
                                (CurrentlySelected != null &&
                                 (package.Id == CurrentlySelected.Id || package.PackageId == CurrentlySelected.PackageId)))
                            {
                                continue;
                            }

                            ReplaceProperties(FromPrevious, Ret.Item2);
                        }
                    }
                }
            }
            return true;
        }

        private static void ReplaceProperties(Package? FromPrevious, List<Variance> Variances)
        {
            if (FromPrevious == null) return;

            foreach (Variance v in Variances)
            {
                if (string.IsNullOrEmpty(v.Prop))
                    continue;

                PropertyInfo? pi = FromPrevious.GetType().GetProperty(v.Prop);
                if (pi == null) continue;

                var current = pi.GetValue(FromPrevious, null);
                var newVal = v.ValB;

                if (!object.Equals(current, newVal))
                {
                    pi.SetValue(FromPrevious, newVal, null);
                }
            }
        }

        private static (bool, List<Variance>) ComparePackages(Package p, Package n)
        {
            bool same = true;

            List<Variance> rt = p.Compare(n);
            foreach (Variance v in rt)
            {
                if (v.Prop == "Recipient_Contents") { }
                else
                    same = false;
            }

            return (same, rt);
        }

        public async Task<bool> UpdateRecords(List<Package> packages, int type = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                if (type == -2)
                {
                    Libmiroppb.Log($"Saving Removed Records: {JsonConvert.SerializeObject(packages.Select(p => p.Id))}");
                }
                else
                {
                    Libmiroppb.Log($"Saving {(type == -1 ? "Previous" : "Current")} Records: {JsonConvert.SerializeObject(packages)}");
                }

                foreach (var p in packages)
                {
                    if (p.PackageIDModified && string.IsNullOrWhiteSpace(p.NewPackageId))
                    {
                        Libmiroppb.Log($"Attempt to change Package ID {p.PackageId} to an empty string");
                        MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageIdEmpty", "Package ID is empty. Not saving.")}");
                        return false;
                    }

                    if (p.PackageIDModified)
                    {
                        var newPackageIdLocal = p.NewPackageId?.ToLower();

#if DEBUG
                        var regex = new Regex("");
#else
                        var regex = new Regex("^cv\\d{7,9}us$");
#endif
                        var match = regex.Match(newPackageIdLocal ?? string.Empty);
                        bool forceSave = true;

                        if (!match.Success)
                        {
                            forceSave = MessageBox.Show(
                                string.Format(Loc.Tr("PAS4U.MainWindow.NewWrongPackageIDFormat", "New Package ID isn't in the correct format:\n\nScanned: {0}\n\nAppropriate: CV#########US\n\n Force save?"), newPackageIdLocal),
                                "Incorrect format", MessageBoxButton.YesNo, MessageBoxImage.Warning
                            ) == MessageBoxResult.Yes;
                        }

                        if (!forceSave) return false;

                        Libmiroppb.Log($"Package ID updated from {p.PackageId} to {newPackageIdLocal}");
                    }
                }

                var payload = packages.Select(p => new
                {
                    id = p.Id,
                    packageid = p.PackageId,
                    newpackageid = p.NewPackageId?.ToLower(),
                    packageidmodified = p.PackageIDModified,

                    sender_name = p.Sender_Name,
                    sender_address = p.Sender_Address,
                    sender_phone = p.Sender_Phone,
                    recipient_name = p.Recipient_Name,
                    recipient_address = p.Recipient_Address,
                    recipient_phone = p.Recipient_Phone,
                    weight = p.Weight,
                    value = p.Value,
                    contents = JsonConvert.SerializeObject(p.Recipient_Contents),
                    delivery = p.Delivery,
                    removed = p.Removed
                });

                var response = await _httpClient.PostAsJsonAsync("/update-packages", payload, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Failed to update records: {response.StatusCode} - {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in UpdateRecordsAsync");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                return false;
            }
        }

        public async Task<long> UploadExportedFile(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(fileName);
                var payload = new
                {
                    filename = Path.GetFileName(fileName),
                    content_base64 = Convert.ToBase64String(fileBytes)
                };

                var response = await _httpClient.PostAsJsonAsync("/upload-export", payload, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Failed to upload export: {response.StatusCode} - {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                    return -1;
                }

                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken).ConfigureAwait(false);
                if (result != null && result.TryGetValue("id", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    return idElement.GetInt64();
                }

                return -1;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Error in UploadExportedFileAsync");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                return -1;
            }
        }

        public async Task<bool> VerifyIfExists(string packageid, CancellationToken cancellationToken = default)
        {
            Libmiroppb.Log($"Verifying if package exists: {packageid}");

            try
            {
                var response = await _httpClient.GetAsync($"/package-exists?packageid={Uri.EscapeDataString(packageid)}", cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var json = JsonConvert.DeserializeObject<Dictionary<string, bool>>(content);
                    return json != null && json.TryGetValue("exists", out bool exists) && exists;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Server returned error: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in VerifyIfExists");
            }

            return false;
        }

        public async Task<IEnumerable<Package>> FindRepeatingRecipientsAsync(List<string> package_list, int max_allowed = 3, CancellationToken cancellationToken = default)
        {
            if (package_list == null || package_list.Count == 0)
                return [];

            var payload = new
            {
                package_list,
                max_allowed
            };

            var response = await _httpClient.PostAsJsonAsync("/find-repeating-recipients", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to find repeating recipients: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to find repeating recipients: {response.StatusCode} - {error}");
                return [];
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<Package>>(responseContent) ?? [];
        }

        public async Task<IEnumerable<Export>> FindRecentExports(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/uploaded-exports", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to get recent exports: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to get recent exports: {response.StatusCode} - {error}");
                return [];
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<Export>>(responseContent) ?? [];
        }

        public async Task<Export> DownloadExportAsync(int id, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"/export/{id}", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Libmiroppb.Log($"Failed to download export: {response.StatusCode} - {error}", Libmiroppb.LogType.Error);
                MessageBox.Show($"Failed to download export: {response.StatusCode} - {error}");
                return new Export();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Export>(json) ?? new Export();
        }

        public static string StatusToText(int status)
        {
            switch (status)
            {
                case 1:
                    return Loc.Tr("PAS4U.Export.Status.1", "Scanned New");
                case 2:
                    return Loc.Tr("PAS4U.Export.Status.2", "Scanned Shipped");
                case 3:
                    return Loc.Tr("PAS4U.Export.Status.3", "Scanned Arrived");
                case 4:
                    return Loc.Tr("PAS4U.Export.Status.4", "Scanned Delivered");
                default:
                    return "";
            }
        }
    }
}