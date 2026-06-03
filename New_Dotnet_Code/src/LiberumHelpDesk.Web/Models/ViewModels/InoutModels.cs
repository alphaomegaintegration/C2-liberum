namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class InoutRow
{
    public int Sid { get; init; }
    public string Firstname { get; init; } = "";
    public string Lastname { get; init; } = "";
    public int StatusCode { get; init; }
    public string StatusText { get; init; } = "";
    public string StatusDate { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Uid { get; init; } = "";
    public string Dname { get; init; } = "";
    public bool CanEdit { get; init; }
}

public sealed class InoutBoardVm
{
    public string MFirstname { get; init; } = "";
    public string MLastname { get; init; } = "";
    public string MUid { get; init; } = "";
    public string MDept { get; init; } = "";
    public string MPhone { get; init; } = "";
    public bool MInoutStatusChecked { get; init; }
    public int Sort { get; init; }
    public int Order { get; init; }
    public int FirstNameOrder { get; init; }
    public int LastNameOrder { get; init; }
    public int StatusOrder { get; init; }
    public int PhoneOrder { get; init; }
    public int UserIDOrder { get; init; }
    public int DeptOrder { get; init; }
    public IReadOnlyList<InoutRow> Rows { get; init; } = [];
    public int Count { get; init; }
    public bool ClearForm { get; init; }
}

public sealed class InoutDetailsVm
{
    public int Usid { get; init; }
    public string Uname { get; init; } = "";
    public string Uid { get; init; } = "";
    public string Dname { get; init; } = "";
    public string Email1 { get; init; } = "";
    public string Phone { get; init; } = "";
    public string PhoneHome { get; init; } = "";
    public string PhoneMobile { get; init; } = "";
    public int StatusCode { get; init; }
    public string StatusText { get; init; } = "";
    public string Jobfunction { get; init; } = "";
    public string Userresume { get; init; } = "";
    public bool HasImage { get; init; }
    public bool CanEdit { get; init; }
    public bool CanChangeStatus { get; init; }
}

public sealed class InoutStatusVm
{
    public int Usid { get; init; }
    public string Uname { get; init; } = "";
    public bool Checked { get; init; }
    public string StatusText { get; init; } = "";
    public bool Saved { get; init; }
}

public sealed class InoutUpdateVm
{
    public int Mid { get; init; }
    public string Uname { get; init; } = "";
    public string Uid { get; init; } = "";
    public string Dname { get; init; } = "";
    public string Email1 { get; init; } = "";
    public string Phone { get; init; } = "";
    public string PhoneHome { get; init; } = "";
    public string PhoneMobile { get; init; } = "";
    public string Jobfunction { get; init; } = "";
    public string Userresume { get; init; } = "";
    public bool Saved { get; init; }
    public bool HasImage { get; init; }
    public string MaxImageSize { get; init; } = "";
}
