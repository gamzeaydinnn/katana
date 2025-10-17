using Katana.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Katana.Core.Interfaces;
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

