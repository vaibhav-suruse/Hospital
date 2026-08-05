// Models/DoctorModel.cs
public class DoctorModel
{
    public int Doctor_Id { get; set; }
    public string Name { get; set; }
}

// Lightweight model for Nurse dropdown in Daily Notes
public class NurseDropdownModel
{
    public int NurseId { get; set; }
    public string Name { get; set; }
}
