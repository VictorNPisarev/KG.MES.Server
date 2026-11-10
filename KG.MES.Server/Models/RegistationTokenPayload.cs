namespace KG.MES.Server.Services.Models;

/// <summary>
/// Данные, которые хранятся в registration-токене
/// </summary>
public class RegistrationTokenPayload
{
	public Guid LicenseId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string? ApplicationCode { get; set; }
	public string DeviceHardwareId { get; set; } = string.Empty;
	public string? DeviceName { get; set; }

}