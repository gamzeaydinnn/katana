using System;
using System.Threading.Tasks;

namespace Katana.Business.Interfaces;




public interface IErrorHandler
{
    Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string operationName);

    
    
    
    Task ExecuteWithHandlingAsync(Func<Task> operation, string operationName);
}
