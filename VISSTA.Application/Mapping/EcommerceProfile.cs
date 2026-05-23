using AutoMapper;
using VISSTA.Application.DTOs;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Mapping;

public sealed class EcommerceProfile : Profile
{
    public EcommerceProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<Review, ReviewDto>()
            .ForCtorParam("CustomerName", opt => opt.MapFrom(src => src.Customer == null ? "VISSTA Customer" : src.Customer.FullName))
            .ForCtorParam("ProductName", opt => opt.MapFrom(src => src.Product == null ? "VISSTA Product" : src.Product.Name))
            .ForCtorParam("ProductSlug", opt => opt.MapFrom(src => src.Product == null ? string.Empty : src.Product.Slug));
    }
}
