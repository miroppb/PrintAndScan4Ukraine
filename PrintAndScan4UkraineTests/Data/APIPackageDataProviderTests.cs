using PrintAndScan4Ukraine.Model;

namespace PrintAndScan4Ukraine.Data.Tests
{
    [TestClass()]
    public class APIPackageDataProviderTests
    {
        private APIPackageDataProvider? _provider;

        [TestInitialize]
        public void Setup()
        {
            // WARNING: Use a dedicated test token/environment, not production
            string testToken = "mrVwdROQ9LhmVieIci6weTPC6avvkg1Eb2VlEOU7LLn1cYjmg4dTFcb5R9Udl9k52iuYcwCy9KSJL38caGIkqYQMMMERiSgj4MKI2NPl5BPidmlLd6g0GUFnqG7WcskQ";
            var apiService = new ApiService(testToken);
            _provider = new APIPackageDataProvider(apiService);
        }

        [TestMethod()]
        public async Task FindMissingPackagesTest()
        {
            var result = await _provider!.FindMissingPackages(["cv823697260us", "cv823697261us", "cv823702787us"]);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(result.ToList()[0].InPackages);
            Assert.IsFalse(result.ToList()[1].InPackages);
            Assert.IsTrue(result.ToList()[2].InPackages);
        }

        [TestMethod()]
        public async Task GetAllAsyncTest()
        {
            var result = await _provider!.GetAllAsync(initialLoad: true);

            Assert.IsNotNull(result);
            Assert.IsGreaterThan(1, result.Count());
            Assert.AreEqual(6, result.First().Id);
            Assert.AreEqual("cv3003107us", result.First().PackageId);
        }

        [TestMethod()]
        public async Task GetAllStatusesTest()
        {
            var result = await _provider!.GetAllStatuses(["cv3013689us"], false);

            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetByNameAsyncTest()
        {
            var result = await _provider!.GetByNameAsync("Darina Oliinyk", false);

            Assert.IsNotNull(result);
            Assert.Contains("Darina Oliinyk", result.First().Sender_Name);
        }

        [TestMethod()]
        public async Task GetPackagesAsyncTest()
        {
            var result = await _provider!.GetPackagesAsync(["test123", "testabc", "cv823697260us"], false);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.Contains("test123", result.Select(x => x.PackageId));
            Assert.Contains("testabc", result.Select(x => x.PackageId));
            Assert.Contains("cv823697260us", result.Select(x => x.PackageId));
        }

        [TestMethod()]
        public async Task GetPackagesByDateAndLastStatusAsyncTest()
        {
            var result = await _provider!.GetPackagesByDateAndLastStatusAsync(DateTime.Parse("2025-07-01T00:00:00"), DateTime.Parse("2025-08-01T23:59:59"), 1);
            var result2 = await _provider!.GetStatusByPackage(result!.First().PackageId);

            Assert.IsNotNull(result);
            Assert.IsGreaterThan(DateTime.Parse("2025-07-01T00:00:00"), result2!.First().CreatedDate);
            Assert.IsLessThanOrEqualTo(DateTime.Parse("2025-08-01T23:59:59"), result2!.First().CreatedDate);
        }

        [TestMethod()]
        public async Task GetServerDateTest()
        {
            var result = await _provider!.GetServerDate();

            Assert.IsNotNull(result);
            Assert.IsGreaterThan(DateTime.Now, result);
        }

        [TestMethod()]
        public async Task GetStatusByPackageTest()
        {
            var result = await _provider!.GetStatusByPackage("cv3013689us");

            Assert.IsNotNull(result);
            Assert.AreEqual("cv3013689us", result.First().PackageId);
            Assert.AreEqual(1, result.GroupBy(x => x.PackageId).Count());
        }

        [TestMethod()]
        public async Task GetUserIDsAndNamesTest()
        {
            var result = await _provider!.GetUserIDsAndNames();

            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task InsertRecordTest()
        {
            Random r = new();
            int ran = r.Next();
            var result = await _provider!.InsertRecord(new Package() { PackageId = $"apitest{ran}", Sender_Name = "John Doe", Sender_Address = "123 Kyiv Street", Sender_Phone = "+380501234567" });
            var result2 = await _provider!.GetPackageAsync($"apitest{ran}", false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result);
            Assert.IsNotNull(result2);
            Assert.AreEqual("John Doe", result2.First().Sender_Name);
        }

        [TestMethod()]
        public async Task InsertRecordStatusTest()
        {
            Random r = new();
            int ran = r.Next();
            var result = await _provider!.InsertRecordStatus([new() { PackageId = $"apitest{ran}", Createdbyuser = 1, CreatedDate = DateTime.Now, Status = 1 }]);
            var result2 = await _provider!.GetStatusByPackage($"apitest{ran}");

            Assert.IsNotNull(result);
            Assert.IsTrue(result);
            Assert.IsNotNull(result2);
            Assert.AreEqual(1, result2.First().Createdbyuser);
        }

        [TestMethod()]
        public async Task UpdateRecordsTest()
        {
            var result = await _provider!.UpdateRecords([new() { Id = 12776, PackageId = "testmiro123-api", Sender_Name = "John Doe1", Sender_Address = "123 Kyiv Street", Sender_Phone = "+380501234567" }]);
            var result2 = await _provider!.GetPackageAsync("testmiro123-api", false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result);
            Assert.IsNotNull(result2);
            Assert.AreEqual("John Doe1", result2.First().Sender_Name);
        }

        [TestMethod()]
        public async Task VerifyIfExistsTest()
        {
            var result = await _provider!.VerifyIfExists("testmiro123-api");

            Assert.IsNotNull(result);
            Assert.IsTrue(result);
        }
    }
}