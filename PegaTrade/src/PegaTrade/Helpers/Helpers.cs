using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PegaTrade.Helpers
{
    public static class Helpers
    {
        /// <summary>
        /// Converts any type of collection into a SelectList collection.
        /// </summary>
        /// <param name="result">The collection to convert</param>
        /// <param name="textProperty">The property to use as text</param>
        /// <param name="valueProperty">The property to use as value</param>
        /// <param name="selectedValue">The current selected value of this list</param>
        public static IEnumerable<SelectListItem> ToSelectList<T>(this IEnumerable<T> result, Func<T, string> textProperty, Func<T, string> valueProperty, string selectedValue = null)
        {
            if (result == null) { return new List<SelectListItem>(); }
            IEnumerable<SelectListItem> list = result.Select(r => new SelectListItem
            {
                Text = textProperty(r),
                Value = valueProperty(r),
                Selected = selectedValue == valueProperty(r)
            });

            return list.ToList();
        }

        public static IEnumerable<SelectListItem> ToSelectList<T>(this IEnumerable<T> result, Func<T, string> textProperty)
        {
            return ToSelectList(result, textProperty, textProperty);
        }

        public static string GetAllErrorsString(this ModelStateDictionary modelStateDictionary)
        {
            if (modelStateDictionary == null) { return string.Empty; }
            return string.Join(" | ", modelStateDictionary.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }
    }

    [HtmlTargetElement("input", Attributes = ForAttributeName)]
    public class PegaCustomInputHelper : InputTagHelper
    {
        private const string ForAttributeName = "asp-for";

        [HtmlAttributeName("asp-disabled")]
        public bool IsDisabled { set; get; }

        public PegaCustomInputHelper(IHtmlGenerator generator) : base(generator)
        {
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (IsDisabled)
            {
                var d = new TagHelperAttribute("disabled", "disabled");
                output.Attributes.Add(d);
            }
            base.Process(context, output);
        }
    }
}