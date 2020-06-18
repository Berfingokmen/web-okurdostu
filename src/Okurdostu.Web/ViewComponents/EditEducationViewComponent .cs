﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okurdostu.Data;
using Okurdostu.Web.Extensions;
using System.Threading.Tasks;

namespace Okurdostu.Web.ViewComponents
{
    [ViewComponent(Name = "EditEducation")]
    public class EditEducationViewComponent : ViewComponent
    {
        private readonly OkurdostuContext Context;
        public EditEducationViewComponent(OkurdostuContext _context) => Context = _context;

        public async Task<IViewComponentResult> InvokeAsync(long EducationdId)
        {
            var Education = await Context.UserEducation.FirstOrDefaultAsync(x => x.Id == EducationdId);
            if (Education != null)
            {
                var Model = new EducationModel
                {
                    EducationId = EducationdId,
                    ActivitiesSocieties = Education.ActivitiesSocieties,
                    Startyear = int.Parse(Education.StartYear),
                    Finishyear = int.Parse(Education.EndYear),
                };
                Model.ListYears();
                if (Education.IsUniversityInformationsCanEditable())
                {
                    Model.UniversityId = Education.UniversityId;
                    Model.Department = Education.Department;

                    await Model.ListUniversitiesAsync();
                }
                return View(Model);
            }
            return null;
        }
    }
}
