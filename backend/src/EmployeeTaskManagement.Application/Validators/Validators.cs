using FluentValidation;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Core.Enums;

namespace EmployeeTaskManagement.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("Username or email is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public class UserCreateValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(100).WithMessage("Full Name cannot exceed 100 characters.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("A valid Role must be selected.");
        }
    }

    public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid user identifier is required.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(100).WithMessage("Full Name cannot exceed 100 characters.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("A valid Role must be selected.");

            When(x => !string.IsNullOrEmpty(x.Password), () =>
            {
                RuleFor(x => x.Password!)
                    .MinimumLength(6).WithMessage("New password must be at least 6 characters.");
            });
        }
    }

    public class TaskCreateValidator : AbstractValidator<TaskCreateDto>
    {
        public TaskCreateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

            RuleFor(x => x.Priority)
                .Must(p => TaskPriorities.All.Contains(p))
                .WithMessage("Priority must be Low, Medium, or High.");

            RuleFor(x => x.Status)
                .Must(s => TaskStatuses.All.Contains(s))
                .WithMessage("Status must be New, In Progress, or Completed.");

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("Due Date must be equal to or after Start Date.");

            RuleFor(x => x.AssignedToUserId)
                .GreaterThan(0).WithMessage("Task must be assigned to a valid employee.");
        }
    }

    public class TaskUpdateValidator : AbstractValidator<TaskUpdateDto>
    {
        public TaskUpdateValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid task identifier is required.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

            RuleFor(x => x.Priority)
                .Must(p => TaskPriorities.All.Contains(p))
                .WithMessage("Priority must be Low, Medium, or High.");

            RuleFor(x => x.Status)
                .Must(s => TaskStatuses.All.Contains(s))
                .WithMessage("Status must be New, In Progress, or Completed.");

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("Due Date must be equal to or after Start Date.");

            RuleFor(x => x.AssignedToUserId)
                .GreaterThan(0).WithMessage("Task must be assigned to a valid employee.");
        }
    }

    public class TaskCommentCreateValidator : AbstractValidator<TaskCommentCreateDto>
    {
        public TaskCommentCreateValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0).WithMessage("Valid task identifier is required.");

            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Comment text cannot be empty.")
                .MaximumLength(2000).WithMessage("Comment text cannot exceed 2000 characters.");
        }
    }
}
