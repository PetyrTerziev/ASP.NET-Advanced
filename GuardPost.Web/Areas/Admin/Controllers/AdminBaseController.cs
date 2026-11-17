using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Web.Controllers;

namespace GuardPost.Web.Areas.Admin.Controllers;

/// <summary>Every controller in the Admin area requires the Administrator role.</summary>
[Area(RoleNames.AdminArea)]
[Authorize(Roles = RoleNames.Administrator)]
public abstract class AdminBaseController : BaseController
{
}
