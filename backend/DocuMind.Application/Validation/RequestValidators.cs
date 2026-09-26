using DocuMind.Application.DTOs.Auth;
using DocuMind.Application.DTOs.Conversations;
using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.DTOs.Search;
using FluentValidation;

namespace DocuMind.Application.Validation;

/*
 * Every rule about what a request may contain, in one file.
 *
 * These replace the attributes that used to sit on the DTOs rather than joining them: two validation
 * systems on one class means two places a rule can be changed and one of them forgotten. Attributes
 * also could not express the rules that matter most here — a password that has to contain more than
 * one kind of character, or a question that must not be only punctuation.
 *
 * The messages are written for the person who will read them, not for a developer.
 */

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Enter your name.")
            .MinimumLength(2).WithMessage("That name is too short.")
            .MaximumLength(120).WithMessage("That name is too long.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Enter your email address.")
            .EmailAddress().WithMessage("That does not look like an email address.")
            .MaximumLength(256).WithMessage("That email address is too long.");

        RuleFor(request => request.Password).Password();
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Deliberately lax: the rules that apply when choosing a password would, on the way in, tell
        // someone whether a password could ever have been correct.
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Enter your email address.")
            .EmailAddress().WithMessage("That does not look like an email address.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Enter your password.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty().WithMessage("Enter your current password.");

        RuleFor(request => request.NewPassword).Password();

        RuleFor(request => request.NewPassword)
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("Choose a password you have not used here before.");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Enter your name.")
            .MinimumLength(2).WithMessage("That name is too short.")
            .MaximumLength(120).WithMessage("That name is too long.");
    }
}

public class UploadTicketRequestValidator : AbstractValidator<UploadTicketRequest>
{
    public UploadTicketRequestValidator()
    {
        RuleFor(request => request.FileName).DocumentFileName();

        RuleFor(request => request.FileSize)
            .GreaterThan(0).WithMessage("That file appears to be empty.");
    }
}

public class ConfirmUploadRequestValidator : AbstractValidator<ConfirmUploadRequest>
{
    public ConfirmUploadRequestValidator()
    {
        RuleFor(request => request.PublicId)
            .NotEmpty().WithMessage("The upload reference is missing.")
            .MaximumLength(400).WithMessage("The upload reference is not valid.");

        RuleFor(request => request.FileName).DocumentFileName();
    }
}

public class SemanticSearchRequestValidator : AbstractValidator<SemanticSearchRequest>
{
    public SemanticSearchRequestValidator()
    {
        RuleFor(request => request.Query).Question();

        RuleFor(request => request.TopK)
            .InclusiveBetween(1, 50).WithMessage("Ask for between 1 and 50 results.")
            .When(request => request.TopK.HasValue);
    }
}

public class DocumentChatRequestValidator : AbstractValidator<DocumentChatRequest>
{
    public DocumentChatRequestValidator()
    {
        RuleFor(request => request.Question).Question();
    }
}

public class StartConversationRequestValidator : AbstractValidator<StartConversationRequest>
{
    public StartConversationRequestValidator()
    {
        RuleFor(request => request.DocumentId)
            .NotEmpty().WithMessage("Choose a document to ask about.");

        // Optional here: a conversation can be opened without asking anything yet.
        RuleFor(request => request.Question!).Question().When(
            request => !string.IsNullOrWhiteSpace(request.Question));
    }
}

public class AskInConversationRequestValidator : AbstractValidator<AskInConversationRequest>
{
    public AskInConversationRequestValidator()
    {
        RuleFor(request => request.Question).Question();
    }
}

/// <summary>Rules used by more than one request, so they cannot drift apart.</summary>
internal static class SharedRules
{
    /// <summary>
    /// A password that is long enough and mixed enough to be worth hashing. Length does most of the
    /// work; the variety rule exists to rule out the worst of the short-and-obvious.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Choose a password.")
            .MinimumLength(8).WithMessage("Use at least 8 characters.")
            .MaximumLength(128).WithMessage("That password is too long.")
            .Must(password => password.Any(char.IsLetter) && password.Any(character => !char.IsLetter(character)))
            .WithMessage("Mix letters with at least one number or symbol.");
    }

    /// <summary>A question a person actually typed, rather than a stray keystroke or only punctuation.</summary>
    public static IRuleBuilderOptions<T, string> Question<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Type a question first.")
            .MinimumLength(3).WithMessage("That question is too short.")
            .MaximumLength(2000).WithMessage("That question is too long — try asking it in fewer words.")
            .Must(question => question.Any(char.IsLetterOrDigit))
            .WithMessage("That question needs some words in it.");
    }

    /// <summary>A file name that is safe to store and points at something this app can read.</summary>
    public static IRuleBuilderOptions<T, string> DocumentFileName<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("The file name is missing.")
            .MaximumLength(260).WithMessage("That file name is too long.")
            // Path separators and traversal are rejected here as well as in storage: the same claim
            // is checked at both ends, and a rejected request never reaches the disk at all.
            .Must(name => !name.Contains('/') && !name.Contains('\\') && !name.Contains(".."))
            .WithMessage("That file name is not allowed.");
    }
}
