namespace BlogSystem.Models
{
    using System.Collections.Generic;
    using System.ComponentModel;

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