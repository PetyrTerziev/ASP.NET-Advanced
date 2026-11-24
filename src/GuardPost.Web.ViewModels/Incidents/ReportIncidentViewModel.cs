using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

using GuardPost.Common;
using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Validation;

using static GuardPost.Common.EntityValidationConstants.Incident;

namespace GuardPost.Web.ViewModels.Incidents;

/// <summary>Simplified incident form for guards (status and resolution are handled by the administrators).</summary>
public class ReportIncidentViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(TitleMaxLength, MinimumLength = TitleMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [Display(Name = "Заглавие")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(DescriptionMaxLength, MinimumLength = DescriptionMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [Display(Name = "Описание")]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [NotInFuture]
    [Display(Name = "Дата и час на събитието")]
    public DateTime? OccurredOn { get; set; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(IncidentSeverity))]
    [Display(Name = "Сериозност")]
    public IncidentSeverity? Severity { get; set; } = IncidentSeverity.Medium;

    [Required(ErrorMessage = ValidationMessages.SelectSite)]
    [Display(Name = "Обект")]
    public int? SiteId { get; set; }

    [BindNever]
    [ValidateNever]
    public IEnumerable<DropdownItemViewModel> Sites { get; set; } = new List<DropdownItemViewModel>();
}
