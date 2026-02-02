using AutoMapper;
using System.Transactions;
using TransactionAggregatorAPI.DataAccess.Entities;
using TransactionAggregatorAPI.Domain.Models;
using Transaction = TransactionAggregatorAPI.Domain.Models.Transaction;

namespace TransactionAggregatorAPI.DataAccess.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // Entity to Domain mappings
            CreateMap<TransactionEntity, Transaction>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (TransactionType)src.Type))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (TransactionCategory)src.Category))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (Domain.Models.TransactionStatus)src.Status));

            // Domain to Entity mappings
            CreateMap<Transaction, TransactionEntity>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (int)src.Category))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status));
        }
    }
}
