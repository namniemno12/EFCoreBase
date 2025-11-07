using Microsoft.EntityFrameworkCore;
using MyProject.Application.Services.Interfaces;
using MyProject.Domain.Entities;
using MyProject.Helper.Utils.Interfaces;
using MyProject.Infrastructure;

namespace MyProject.Application.Services
{
    public class TesttiepService : ITesttiepService
    {
        private readonly IRepositoryAsync<Testtiep> _repository;
        private readonly ITokenUtils _tokenUtils;
        public TesttiepService(IRepositoryAsync<Testtiep> repository, ITokenUtils tokenUtils)
        {
            _repository = repository;
            _tokenUtils = tokenUtils;
        }

        public async Task<List<Testtiep>> GetAllAsync()
        {
            return await _repository.AsNoTrackingQueryable().ToListAsync();
        }

        public async Task<Testtiep> CreateAsync(Testtiep entity)
        {
            entity.Id = Guid.NewGuid();
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            return entity;
        }
    }
}
