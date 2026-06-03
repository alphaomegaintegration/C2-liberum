namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class UserListRowVm
{
    public int Sid { get; init; }
    public string Uid { get; init; } = "";
    public string Fname { get; init; } = "";
}

public sealed class UserFormVm
{
    public bool IsEdit { get; init; }
    public int ModSid { get; init; }
    public string Uid { get; init; } = "";
    public bool Success { get; init; }
    public bool AccountDeleted { get; init; }
    public bool RepProbsError { get; init; }

    public string Email { get; init; } = "";
    public string Firstname { get; init; } = "";
    public string Lastname { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Location { get; init; } = "";
    public string Pager { get; init; } = "";
    public string PhoneHome { get; init; } = "";
    public string PhoneMobile { get; init; } = "";
    public string Jobfunction { get; init; } = "";
    public string Userresume { get; init; } = "";
    public string Statustext { get; init; } = "";
    public int Department { get; init; }
    public int UsrLanguage { get; init; }
    public int RepAccess { get; init; }
    public int Statuscode { get; init; }
    public int ListOnInoutBoard { get; init; } = 1;
    public bool IsRep { get; init; }
    public bool InoutAdmin { get; init; }

    public bool ShowPager { get; init; }
    public bool UseInout { get; init; }
    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<LangOpt> Languages { get; init; } = [];
}
