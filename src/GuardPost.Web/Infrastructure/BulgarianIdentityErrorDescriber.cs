using Microsoft.AspNetCore.Identity;

namespace GuardPost.Web.Infrastructure;

/// <summary>Bulgarian texts for the Identity validation errors (polymorphism – overrides the English defaults).</summary>
public class BulgarianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = $"Имейлът {email} вече е регистриран." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = $"Потребителското име {userName} е заето." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = "Невалиден имейл адрес." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = $"Паролата трябва да е поне {length} символа." };

    public override IdentityError PasswordRequiresDigit()
        => new() { Code = nameof(PasswordRequiresDigit), Description = "Паролата трябва да съдържа поне една цифра." };

    public override IdentityError PasswordRequiresLower()
        => new() { Code = nameof(PasswordRequiresLower), Description = "Паролата трябва да съдържа поне една малка буква." };

    public override IdentityError PasswordRequiresUpper()
        => new() { Code = nameof(PasswordRequiresUpper), Description = "Паролата трябва да съдържа поне една главна буква." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Паролата трябва да съдържа поне един специален символ." };

    public override IdentityError UserAlreadyInRole(string role)
        => new() { Code = nameof(UserAlreadyInRole), Description = "Потребителят вече има тази роля." };

    public override IdentityError UserNotInRole(string role)
        => new() { Code = nameof(UserNotInRole), Description = "Потребителят няма тази роля." };

    public override IdentityError DefaultError()
        => new() { Code = nameof(DefaultError), Description = "Възникна неочаквана грешка." };
}
