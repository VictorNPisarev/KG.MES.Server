using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Shared.Models.Enums;

public enum PermissionType
{
	[PermissionName("CreateLicense")]
	CreateLicense,

	[PermissionName("RevokeLicense")]
	RevokeLicense,

	[PermissionName("ExtendLicense")]
	ExtendLicense,

	[PermissionName("CreateUser")]
	CreateUser,

	[PermissionName("BlockUser")]
	BlockUser,

	[PermissionName("SetRole")]
	SetRole,

	[PermissionName("ResetPassword")]
	ResetPassword,

	[PermissionName("ViewLicenses")]
	ViewLicenses,

	[PermissionName("ViewUsers")]
	ViewUsers
}