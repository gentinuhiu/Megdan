namespace Megdan.Web.ViewModels
{
    public class UserComplaintsViewModel
    {
        public string Email { get; set; } = string.Empty;
        public List<UserComplaintItemViewModel> Complaints { get; set; } = new();
    }
}