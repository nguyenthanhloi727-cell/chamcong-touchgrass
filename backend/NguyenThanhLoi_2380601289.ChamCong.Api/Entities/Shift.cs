namespace NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

public class Shift
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
