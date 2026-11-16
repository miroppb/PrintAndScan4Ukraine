using PrintAndScan4Ukraine.Model;
using System.Threading;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Data
{
    public interface IMainDataProvider
    {
        Task<Users> GetUserFromComputerNameAsync(string ComputerName, CancellationToken cancellationToken = default);
        Task Heartbeat(Users user, CancellationToken cancellationToken = default);
    }
}