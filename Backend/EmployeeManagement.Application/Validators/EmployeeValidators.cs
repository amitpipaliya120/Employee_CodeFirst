using FluentValidation;
using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Validators;

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Employee Name is required.")
            .MaximumLength(150).WithMessage("Employee Name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[0-9+\-\s()]{7,20}$").WithMessage("Phone number must be valid (minimum 7 digits).");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required.")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation cannot exceed 100 characters.");

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining Date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1)).WithMessage("Joining Date cannot be more than 1 year in the future.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s == "Active" || s == "Inactive")
            .WithMessage("Status must be Active or Inactive.");
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("A valid Employee ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Employee Name is required.")
            .MaximumLength(150).WithMessage("Employee Name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[0-9+\-\s()]{7,20}$").WithMessage("Phone number must be valid (minimum 7 digits).");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required.")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation cannot exceed 100 characters.");

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining Date is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s == "Active" || s == "Inactive")
            .WithMessage("Status must be Active or Inactive.");
    }
}
