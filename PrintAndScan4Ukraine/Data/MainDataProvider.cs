using PrintAndScan4Ukraine.Model;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Data
{
    public interface IMainDataProvider
    {
        Task<Users> GetUserFromComputerNameAsync(string ComputerName);
        void Heartbeat(Users user);
    }
}
