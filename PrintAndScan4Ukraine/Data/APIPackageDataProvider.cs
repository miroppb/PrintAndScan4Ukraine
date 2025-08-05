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
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
    public class APIPackageDataProvider(IApiService apiService) : IPackageDataProvider
    {
        private readonly HttpClient _httpClient = apiService.Client;

        public async Task<IEnumerable<MissingPackages>> FindMissingPackages(List<string> barcodesNotInPackages)
        {
            var requestBody = new
            {
                barcodes = barcodesNotInPackages
            };

            var response = await _httpClient.PostAsync(
                "/find-missing-packages",
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get missing packages: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<MissingPackages>>(json) ?? new List<MissingPackages>();
        }

        public async Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad)
        {
            Libmiroppb.Log($"Get List of Packages{(initialLoad ? "" : " for a refresh")}");
            IEnumerable<Package> packages = [];
            try
            {
                var response = await _httpClient.GetStringAsync("/packages");

                var result = JsonConvert.DeserializeObject<List<Package>>(response);
                if (result != null)
                {
                    packages = [.. result.Select(x =>
                    {
                        x.Recipient_Contents = !string.IsNullOrEmpty(x.Contents)
                            ? JsonConvert.DeserializeObject<List<Contents>>(x.Contents)!
                            : [];
                        return x;
                    })];
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}:\n{ex.Message}");
            }

            return packages;
        }

        public async Task<IEnumerable<Package_Status>?> GetAllStatuses(List<string> ids, bool useArchive = false)
        {
            Libmiroppb.Log("Get List of Packages Statuses");

            IEnumerable<Package_Status>? statuses = [];

            try
            {
                var requestBody = new
                {
                    ids,
                    use_archive = useArchive
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/package-statuses", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    statuses = JsonConvert.DeserializeObject<List<Package_Status>>(json);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Libmiroppb.Log($"Server returned error: {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"Exception: {ex.Message}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}:\n{ex.Message}");
            }

            return statuses;
        }


        public async Task<IEnumerable<Package>?> GetByNameAsync(string SenderName, bool useArchive = false)
        {
            var payload = new
            {
                sender_name = SenderName,
                use_archive = useArchive
            };

            var response = await _httpClient.PostAsync(
                "/packages-by-name",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API call failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? [];

            foreach (var pkg in packages)
            {
                if (!string.IsNullOrWhiteSpace(pkg.Contents))
                {
                    try
                    {
                        pkg.Recipient_Contents = JsonConvert.DeserializeObject<List<Contents>>(pkg.Contents) ?? [];
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

        public async Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive)
        {
            var payload = new
            {
                packageid,
                use_archive = useArchive
            };

            var response = await _httpClient.PostAsync(
                "/package-by-id",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get package: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? new();

            return packages;
        }

        public async Task<IEnumerable<Package>> GetPackagesAsync(List<string> package_ids, bool useArchive = false)
        {
            if (package_ids == null || package_ids.Count == 0)
                return [];

            var payload = new
            {
                package_ids,
                use_archive = useArchive
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/packages-batch", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Package>>(responseContent)!;
        }

        public async Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start_date, DateTime end_date, int status_code)
        {
            var payload = new
            {
                start_date,
                end_date,
                status_code
            };

            var response = await _httpClient.PostAsync(
                "/packages-by-date-and-status",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                Libmiroppb.Log($"Error retrieving packages: {response.StatusCode}");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var packages = JsonConvert.DeserializeObject<List<Package>>(json) ?? [];

            return packages;
        }

        public async Task<DateTime> GetServerDate()
        {
            var response = await _httpClient.GetAsync("/server-date");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get server date: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (result != null)
                return DateTime.Parse(result["server_time"]);
            else
                return DateTime.Now;
        }

        public async Task<IEnumerable<Package_Status>?> GetStatusByPackage(string packageid)
        {
            Libmiroppb.Log($"Get List of Package Statuses for {packageid}");
            IEnumerable<Package_Status>? statuses = [];
            try
            {
                var response = await _httpClient.GetAsync($"/package-status?packageid={Uri.EscapeDataString(packageid)}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    statuses = JsonConvert.DeserializeObject<List<Package_Status>>(json);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Libmiroppb.Log($"Server returned error: {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                }

                Libmiroppb.Log(JsonConvert.SerializeObject(statuses));
            }
            catch
            {
                Libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
            }
            return statuses;
        }

        public async Task<IEnumerable<Users>> GetUserIDsAndNames()
        {
            var response = await _httpClient.GetAsync("/user-ids-and-names");

            if (!response.IsSuccessStatusCode)
            {
                Libmiroppb.Log($"Error retrieving user IDs and names: {response.StatusCode}");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<Users>>(json) ?? [];

            return users;
        }

        public async Task<bool> InsertRecord(Package package)
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

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/insert-package", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InsertRecordStatus(List<Package_Status> statuses)
        {
            var payload = statuses.Select(p => new
            {
                id = p.Id,
                packageid = p.PackageId,
                createdbyuser = p.Createdbyuser,
                createddate = p.CreatedDate,
                status = p.Status
            });

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/insert-package-statuses", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Libmiroppb.Log($"Failed to insert statuses: {response.StatusCode} - {error}");
                return false;
            }

            return true;
        }

        public List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses) //no need
        {
            List<Package_less> list = [];
            foreach (Package package in packages)
            {
                Package temp = package.CreateCopy();
                List<Contents> t = package.Recipient_Contents.CreateCopy();
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

        public async Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected) //no need
        {
            if (CurrentlySelected == null) return false;
            IEnumerable<Package>? RefreshedPackages = await GetAllAsync(false);
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
                            if (package.Id != CurrentlySelected.Id) //Refresh only for non-current package. Otherwise could cause discrepencies
                                ReplaceProperties(FromPrevious, Ret.Item2);
                        }
                    }
                }
            }
            return true;
        }

        private static void ReplaceProperties(Package? FromPrevious, List<Variance> Variances) //no need
        {
            foreach (Variance v in Variances)
            {
                PropertyInfo pi = FromPrevious!.GetType().GetProperty(v.Prop!)!;
                pi.SetValue(FromPrevious, v.ValB, null);
            }
        }

        private static (bool, List<Variance>) ComparePackages(Package p, Package n) //no need
        {
            bool same = true;

            List<Variance> rt = p.Compare(n);
            foreach (Variance v in rt)
            {
                if (v.Prop == "Recipient_Contents") { } //Doesn't compare well the Recipient Contents for some reason
                else
                    same = false;
            }

            return (same, rt);
        }

        public async Task<bool> UpdateRecords(List<Package> packages, int type = 0)
        {
            try
            {
                packages.ForEach(p => p.Contents = JsonConvert.SerializeObject(p.Recipient_Contents));

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
                        p.NewPackageId = p.NewPackageId.ToLower();

#if DEBUG
                        var regex = new Regex("");
#else
                var regex = new Regex("^cv\\d{7,9}us$");
#endif
                        var match = regex.Match(p.NewPackageId);
                        bool forceSave = true;

                        if (!match.Success)
                        {
                            forceSave = MessageBox.Show(
                                string.Format(Loc.Tr("PAS4U.MainWindow.NewWrongPackageIDFormat", "New Package ID isn't in the correct format:\n\nScanned: {0}\n\nAppropriate: CV#########US\n\n Force save?"), p.NewPackageId),
                                "Incorrect format", MessageBoxButton.YesNo, MessageBoxImage.Warning
                            ) == MessageBoxResult.Yes;
                        }

                        if (!forceSave) return false;

                        Libmiroppb.Log($"Package ID updated from {p.PackageId} to {p.NewPackageId}");
                        //p.PackageId = p.NewPackageId;
                        //p.PackageIDModified = false;
                        //this happens in the api
                    }
                }

                var payload = packages.Select(p => new
                {
                    id = p.Id,
                    packageid = p.PackageId,
                    newpackageid = p.NewPackageId,
                    packageidmodified = p.PackageIDModified,

                    sender_name = p.Sender_Name,
                    sender_address = p.Sender_Address,
                    sender_phone = p.Sender_Phone,
                    recipient_name = p.Recipient_Name,
                    recipient_address = p.Recipient_Address,
                    recipient_phone = p.Recipient_Phone,
                    weight = p.Weight,
                    value = p.Value,
                    contents = p.Contents,
                    delivery = p.Delivery,
                    removed = p.Removed
                });

                var response = await _httpClient.PostAsJsonAsync($"/update-packages", payload);

                if (!response.IsSuccessStatusCode)
                {
                    Libmiroppb.Log($"Failed to update records: {response.StatusCode}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"Exception in UpdateRecordsAsync: {ex.Message}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                return false;
            }
        }

        public async Task<long> UploadExportedFile(string fileName)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(fileName);
                var payload = new
                {
                    filename = Path.GetFileName(fileName),
                    content_base64 = Convert.ToBase64String(fileBytes)
                };

                var response = await _httpClient.PostAsJsonAsync($"/upload-export", payload);

                if (!response.IsSuccessStatusCode)
                {
                    Libmiroppb.Log($"Failed to upload export: {response.StatusCode}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                    return -1;
                }

                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
                if (result != null && result.TryGetValue("id", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    return idElement.GetInt64();
                }

                return -1;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"Error in UploadExportedFileAsync: {ex.Message}");
                MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
                return -1;
            }
        }

        public async Task<bool> VerifyIfExists(string packageid)
        {
            Libmiroppb.Log($"Verifying if package exists: {packageid}");

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/package-exists?packageid={Uri.EscapeDataString(packageid)}");
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    // Expecting { "exists": true } from FastAPI
                    var json = JsonConvert.DeserializeObject<Dictionary<string, bool>>(content);
                    return json != null && json.TryGetValue("exists", out bool exists) && exists;
                }
                else
                {
                    Libmiroppb.Log($"Server returned error: {response.StatusCode}");
                    Debug.WriteLine($"API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }

            return false;
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
