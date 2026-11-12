using AutoMapper;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Core.Entities;

namespace EmployeeTaskManagement.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Role Mappings
            CreateMap<Role, RoleDto>().ReverseMap();

            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty))
                .ReverseMap();

            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => System.DateTime.UtcNow));

            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(_ => System.DateTime.UtcNow));

            // Task Mappings
            CreateMap<TaskEntity, TaskDto>()
                .ForMember(dest => dest.AssignedToUserName, opt => opt.MapFrom(src => src.AssignedToUser != null ? src.AssignedToUser.Username : string.Empty))
                .ForMember(dest => dest.AssignedToFullName, opt => opt.MapFrom(src => src.AssignedToUser != null ? src.AssignedToUser.FullName : string.Empty))
                .ForMember(dest => dest.AssignedToDepartment, opt => opt.MapFrom(src => src.AssignedToUser != null ? src.AssignedToUser.Department : string.Empty))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Username : string.Empty))
                .ForMember(dest => dest.CreatedByFullName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments != null ? src.Comments.Count : 0))
                .ReverseMap();

            CreateMap<TaskCreateDto, TaskEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => System.DateTime.UtcNow));

            CreateMap<TaskUpdateDto, TaskEntity>()
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(_ => System.DateTime.UtcNow));

            // Comment Mappings
            CreateMap<TaskComment, TaskCommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.UserRole, opt => opt.MapFrom(src => src.User != null && src.User.Role != null ? src.User.Role.Name : string.Empty))
                .ReverseMap();

            CreateMap<TaskCommentCreateDto, TaskComment>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => System.DateTime.UtcNow));
        }
    }
}
