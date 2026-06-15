using DonationWorker.Domain.Shared.Entity;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonationWorker.Infrastructure.Repositories
{
    public class EFRepository<T> : IRepository<T> where T : EntityBase
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public EFRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AlterarAsync(T entidade, CancellationToken cancellationToken = default)
        {
            entidade.DataModificacao = DateTime.UtcNow;

            _context.Entry(entidade).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Erro ao alterar: {message}", ex);
            }
        }

        public async Task CadastrarAsync(T entidade, CancellationToken cancellationToken = default)
        {
            entidade.DataCriacao = DateTime.UtcNow;

            await _dbSet.AddAsync(entidade, cancellationToken);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Erro ao cadastrar: {message}", ex);
            }
        }

        public async Task RemoverAsync(Guid guid, CancellationToken cancellationToken = default)
        {
            var entidade = await ObterPorGuidAsync(guid, cancellationToken);

            if (entidade != null)
            {
                _dbSet.Remove(entidade);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var message = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Erro ao remover: {message}", ex);
                }
            }
        }

        public async Task<T?> ObterPorGuidAsync(Guid guid, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Guid == guid, cancellationToken);
        }

        public async Task<IList<T>> ObterTodosAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }
    }
}
