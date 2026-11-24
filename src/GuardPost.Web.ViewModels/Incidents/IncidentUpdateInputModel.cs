using System.ComponentModel.DataAnnotations;

using GuardPost.Common;

using static GuardPost.Common.EntityValidationConstants.IncidentUpdate;

namespace GuardPost.Web.ViewModels.Incidents;

public class IncidentUpdateInputModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(TextMaxLength, MinimumLength = TextMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [Display(Name = "Текст")]
    public string Text { get; set; } = null!;
}
