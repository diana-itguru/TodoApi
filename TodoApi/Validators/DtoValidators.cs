using FluentValidation;
using TodoApi.DTOs;

namespace TodoApi.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Некорректный формат Email");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Пароль должен быть не менее 6 символов");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Некорректный формат Email");
    }
}