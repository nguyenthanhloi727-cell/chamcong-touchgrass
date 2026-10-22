using System.ComponentModel.DataAnnotations;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;

public class ChangeRoleRequest
{
    [Required] public Role? Role { get; set; }

    /// <summary>Bắt buộc với Manager/Employee (phải cùng RoleGroup); Admin thì bỏ trống.</summary>
    public int? PositionId { get; set; }
}

public record EmployeeDto(
    int Id, string Username, string FullName, string? Email, string? PhoneNumber,
    string Role, int? PositionId, string? Position, int? DepartmentId, string Status);

public record PositionDto(int Id, string Name, string RoleGroup);
