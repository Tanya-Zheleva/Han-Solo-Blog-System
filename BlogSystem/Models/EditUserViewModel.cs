using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Web.UI.WebControls;

namespace BlogSystem.Models
{
    public class EditUserViewModel
    {
        public ApplicationUser User { get; set; }

        public string Password { get; set; }

        [DisplayName("Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Password does not match")]
        public string ConfirmPassword { get; set; }

        public IList<Role> Roles { get; set; }       
    }
}