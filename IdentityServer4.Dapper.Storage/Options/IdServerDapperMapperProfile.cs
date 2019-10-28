
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.Dapper.Storage.Options
{
    /*
    public class IdServerDapperMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="ApiResourceMapperProfile"/>
        /// </summary>
        public IdServerDapperMapperProfile()
        {
            CreateMap<Entities.ApiResources, Models.ApiResource>(MemberList.Destination)
                .ConstructUsing(src => new Models.ApiResource())
                .ReverseMap();

            CreateMap<Entities.ApiClaims, string>()
                .ConstructUsing(x => x.Type)
                .ReverseMap()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ApiSecrets, Models.Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null))
                .ReverseMap();

            CreateMap<Entities.ApiScopes, Models.Scope>(MemberList.Destination)
                .ConstructUsing(src => new Models.Scope())
                .ReverseMap();

            CreateMap<Entities.ApiScopeClaims, string>()
                .ConstructUsing(x => x.Type)
                .ReverseMap()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src));


            CreateMap<Entities.ClientProperties, KeyValuePair<string, string>>()
            .ReverseMap();


            CreateMap<Entities.Clients, Models.Client>()
                .ForMember(dest => dest.ProtocolType, opt => opt.Condition(srs => srs != null))
                .ReverseMap();

            CreateMap<Entities.ClientCorsOrigins, string>()
                .ConstructUsing(src => src.Origin)
                .ReverseMap()
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ClientIdPRestrictions, string>()
                .ConstructUsing(src => src.Provider)
                .ReverseMap()
                .ForMember(dest => dest.Provider, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ClientClaims, Claim>(MemberList.None)
                .ConstructUsing(src => new Claim(src.Type, src.Value))
                .ReverseMap();

            CreateMap<Entities.ClientScopes, string>()
                .ConstructUsing(src => src.Scope)
                .ReverseMap()
                .ForMember(dest => dest.Scope, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ClientPostLogoutRedirectUris, string>()
                .ConstructUsing(src => src.PostLogoutRedirectUri)
                .ReverseMap()
                .ForMember(dest => dest.PostLogoutRedirectUri, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ClientRedirectUris, string>()
                .ConstructUsing(src => src.RedirectUri)
                .ReverseMap()
                .ForMember(dest => dest.RedirectUri, opt => opt.MapFrom(src => src));

            CreateMap<Entities.ClientGrantTypes, string>()
                .ConstructUsing(src => src.GrantType)
                .ReverseMap()
                .ForMember(dest => dest.GrantType, opt => opt.MapFrom(src => src));

            CreateMap<Entities.IdentityClaims, string>()
    .ConstructUsing(x => x.Type)
    .ReverseMap()
    .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src));


            CreateMap<Entities.ClientSecrets, Models.Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null))
                .ReverseMap();


            CreateMap<Entities.IdentityResources, Models.IdentityResource>(MemberList.Destination)
                .ConstructUsing(src => new Models.IdentityResource())
                .ReverseMap();



            CreateMap<Entities.PersistedGrants, Models.PersistedGrant>(MemberList.Destination)
                .ReverseMap();


        }}
        */
    }
