using KG.MES.Shared.Config;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Enums;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components;

public partial class AdminPage
{
	[Parameter] public AdminPageConfig Config { get; set; } = new();

	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] private AdminService AdminService { get; set; } = null!;

	private bool CanCreateLicense => Config.AllowCreateLicense;
	private bool CanRevokeLicense => Config.AllowRevokeLicense;
	private bool CanExtendLicense => Config.AllowExtendLicense;
	private bool CanCreateUser => Config.AllowCreateUser;
	private bool CanBlockUser => Config.AllowBlockUser;
	private bool CanSetRole => Config.AllowSetRole;
	private bool CanResetPassword => Config.AllowResetPassword;

	private string activeTab = "users";
	private List<LicenseDto> licenses = [];
	private List<UserAdminListItemDto> users = [];
	private bool showCreateLicense;
	private bool showCreateUser;
	private bool showRevokeModal;
	private bool showSetRoleModal;
	private bool isCreating;
	private Guid revokeLicenseId;
	private string revokeReason = "";
	private UserAdminListItemDto? selectedUser;
	private string selectedRole = "User";

	// Новые лицензии
	private LicenseType newLicenseType = LicenseType.SingleDevice;
	private int? newLicenseMaxDevices;
	private int newLicenseDays = 30;
	private string newLicenseNotes = "";

	// Новые пользователи
	private string newUserName = "";
	private string newUserEmail = "";
	private string newUserRole = "User";
	private string newUserPassword = "";

	// Права (частичный доступ)
	private bool IsAdmin => Session.User?.RoleName == "Admin";
	private bool IsSalesManager => Session.User?.RoleName == "SalesManager";

	private bool showViewLicenseModal;
	private bool showExtendModal;
	private LicenseDto? selectedLicense;
	private List<DeviceInfoDto> licenseDevices = [];
	private int extendDays = 30;
	private bool extendUnlimited;
	private bool showResetPasswordModal;
	private string newResetPassword = "";
	private string resetPasswordError = "";
	private bool resetPasswordFieldError;
	private bool selectedUserIsApproved;
	private List<RoleDto> roles = [];
	private List<NotificationSubscriptionDto> subscriptions = [];
	private List<NotificationEventDto> notificationEvents = [];
	private List<UserAdminListItemDto> activeUsers = [];

	private bool showCreateSubscription;
	private bool showDeleteSubscriptionModal;

	private Guid? newSubscriptionEventId;
	private string newSubscriptionTargetType = "role"; // "role" | "user"
	private Guid? newSubscriptionRoleId;
	private Guid? newSubscriptionUserId;
	private string newSubscriptionSeverity = "Info";

	private Guid? subscriptionToDelete;
	private string subscriptionToDeleteLabel = "";

	private static readonly string[] severities = ["Info", "Warning", "Danger", "Success"];

	private bool CanViewNotifications => Config.AllowViewNotifications;
	private bool CanCreateNotification => Config.AllowCreateNotification;
	private bool CanToggleNotification => Config.AllowToggleNotification;
	private bool CanDeleteNotification => Config.AllowDeleteNotification;
	private bool CanEditNotificationSeverity => Config.AllowEditNotificationSeverity;

	protected override async Task OnInitializedAsync()
	{
		roles = await AdminService.GetRolesAsync();
		await LoadLicenses();
		await LoadUsers();

		if (Config.AllowViewNotifications)
		{
			await LoadNotificationEvents();
			await LoadSubscriptions();

			var allUsers = await AdminService.GetUsersAsync(page: 1, limit: 500);
			activeUsers = allUsers.Where(u => u.IsActive).ToList();
		}
	}

	private async Task LoadLicenses()
	{
		licenses = await AdminService.GetLicensesAsync();
	}

	private async Task LoadUsers() => users = await AdminService.GetUsersAsync();

	private void OpenCreateLicenseModal()
	{
		showCreateLicense = true;
		CloseOtherModalsExcept(nameof(showCreateLicense));
	}

	private void OpenCreateUserModal()
	{
		showCreateUser = true;
		CloseOtherModalsExcept(nameof(showCreateUser));
	}

	private void OpenRevokeModal(Guid licenseId)
	{
		revokeLicenseId = licenseId;
		revokeReason = "";
		showRevokeModal = true;
		CloseOtherModalsExcept(nameof(showRevokeModal));
	}

	private void OpenSetRoleModal(UserAdminListItemDto user)
	{
		selectedUser = user;
		selectedRole = user.RoleName ?? "User";
		selectedUserIsApproved = user.IsApproved;
		showSetRoleModal = true;
		CloseOtherModalsExcept(nameof(showSetRoleModal));
	}

	private void CloseModals()
	{
		showCreateLicense = false;
		showCreateUser = false;
		showRevokeModal = false;
		showSetRoleModal = false;
		showViewLicenseModal = false;
		showExtendModal = false;
		showResetPasswordModal = false;
		showCreateSubscription = false;
		showDeleteSubscriptionModal = false;
		subscriptionToDelete = null;
		subscriptionToDeleteLabel = "";
		resetPasswordError = "";
		resetPasswordFieldError = false;
		selectedUser = null;
	}

	private async Task CreateLicense()
	{
		isCreating = true;
		var request = new CreateLicenseRequestDto
		{
			LicenseType = newLicenseType,
			ExpiresDays = newLicenseDays,
			MaxDevices = newLicenseType == LicenseType.MultiDevice ? newLicenseMaxDevices : null,
			Notes = newLicenseNotes
		};

		await AdminService.CreateLicenseAsync(request);
		await LoadLicenses();
		CloseModals();
		isCreating = false;
	}

	private async Task CreateUser()
	{
		isCreating = true;
		var request = new CreateUserRequestDto
		{
			Name = newUserName,
			Email = newUserEmail,
			RoleName = newUserRole,
			Password = string.IsNullOrEmpty(newUserPassword) ? null : newUserPassword
		};

		await AdminService.CreateUserAsync(request);
		await LoadUsers();
		CloseModals();
		isCreating = false;
	}

	private async Task ConfirmRevoke()
	{
		await AdminService.RevokeLicenseAsync(revokeLicenseId, new RevokeLicenseRequestDto { Reason = revokeReason });
		await LoadLicenses();
		CloseModals();
	}

	private async Task ConfirmUpdateProfile()
	{
		if (selectedUser == null) return;

		isCreating = true;
		try
		{
			// 1. Роль — отдельным запросом
			if (selectedUser.RoleName != selectedRole)
			{
				await AdminService.SetUserRoleAsync(selectedUser.Id,
					new SetRoleRequestDto { RoleName = selectedRole });
			}

			// 2. Подтверждение — только если изменилось
			if (selectedUser.IsApproved != selectedUserIsApproved)
			{
				if (selectedUserIsApproved)
					await AdminService.ApproveUserAsync(selectedUser.Id);
				else
					await AdminService.RevokeUserApprovalAsync(selectedUser.Id);
			}


			await LoadUsers();
			CloseModals();
		}
		finally
		{
			isCreating = false;
		}
	}

	private async Task BlockUser(Guid id)
	{
		await AdminService.BlockUserAsync(id);
		await LoadUsers();
	}

	private async Task UnblockUser(Guid id)
	{
		await AdminService.UnblockUserAsync(id);
		await LoadUsers();
	}

	private async Task OpenViewLicenseModal(Guid licenseId)
	{
		selectedLicense = await AdminService.GetLicenseByIdAsync(licenseId);
		licenseDevices = await AdminService.GetLicenseDevicesAsync(licenseId);
		showViewLicenseModal = true;
		CloseOtherModalsExcept(nameof(showViewLicenseModal));
	}

	private void OpenExtendModal()
	{
		showExtendModal = true;
		showViewLicenseModal = false;
	}

	private async Task ConfirmExtend()
	{
		if (selectedLicense != null)
		{
			var days = extendUnlimited ? -1 : extendDays; // -1 = бессрочная
			await AdminService.ExtendLicenseAsync(selectedLicense.Id, days);
			selectedLicense = await AdminService.GetLicenseByIdAsync(selectedLicense.Id);
		}
		showExtendModal = false;
		showViewLicenseModal = true;
		await LoadLicenses();
	}

	private void CloseOtherModalsExcept(string? except = null)
	{
		if (except != nameof(showCreateLicense)) showCreateLicense = false;
		if (except != nameof(showCreateUser)) showCreateUser = false;
		if (except != nameof(showRevokeModal)) showRevokeModal = false;
		if (except != nameof(showSetRoleModal)) showSetRoleModal = false;
		if (except != nameof(showExtendModal)) showExtendModal = false;
		if (except != nameof(showViewLicenseModal)) showViewLicenseModal = false;
		if (except != nameof(showResetPasswordModal)) showResetPasswordModal = false;
		if (except != nameof(showCreateSubscription)) showCreateSubscription = false;
		if (except != nameof(showDeleteSubscriptionModal)) showDeleteSubscriptionModal = false;
	}

	private async Task ToggleUserBlock(UserAdminListItemDto user)
	{
		if (user.IsActive)
			await AdminService.BlockUserAsync(user.Id);
		else
			await AdminService.UnblockUserAsync(user.Id);

		await LoadUsers();
	}

	// Открытие
	private void OpenResetPasswordModal(UserAdminListItemDto user)
	{
		selectedUser = user;
		newResetPassword = "";
		resetPasswordError = "";
		resetPasswordFieldError = false;
		showResetPasswordModal = true;
		CloseOtherModalsExcept(nameof(showResetPasswordModal));
	}

	// Подтверждение
	private async Task ConfirmResetPassword()
	{
		if (selectedUser == null) return;

		resetPasswordError = "";
		resetPasswordFieldError = string.IsNullOrWhiteSpace(newResetPassword);

		if (resetPasswordFieldError)
		{
			resetPasswordError = "Пароль не может быть пустым";
			return;
		}

		if (newResetPassword.Length < 4)
		{
			resetPasswordFieldError = true;
			resetPasswordError = "Пароль должен быть не менее 4 символов";
			return;
		}

		isCreating = true;
		try
		{
			var success = await AdminService.SetUserPasswordAsync(selectedUser.Email, newResetPassword);
			if (success)
			{
				await LoadUsers();
				CloseModals();
			}
			else
			{
				resetPasswordError = "Не удалось сбросить пароль";
			}
		}
		finally
		{
			isCreating = false;
		}
	}

	private async Task OnApprovalChanged(ChangeEventArgs e)
	{
		if (selectedUser == null) return;

		var isApproved = e.Value is bool b && b;

		// Сразу дёргаем API при изменении чекбокса
		isCreating = true;
		try
		{
			if (isApproved)
				await AdminService.ApproveUserAsync(selectedUser.Id);
			else
				await AdminService.RevokeUserApprovalAsync(selectedUser.Id);

			selectedUser.IsApproved = isApproved;
		}
		finally
		{
			isCreating = false;
		}

		StateHasChanged();
	}

	private async Task LoadNotificationEvents() => notificationEvents = await AdminService.GetNotificationEventsAsync();
	private async Task LoadSubscriptions() => subscriptions = await AdminService.GetSubscriptionsAsync();

	private void OpenCreateSubscriptionModal()
	{
		showCreateSubscription = true;
		CloseOtherModalsExcept(nameof(showCreateSubscription));

		newSubscriptionEventId = notificationEvents.FirstOrDefault()?.Id;
		newSubscriptionTargetType = "role";
		newSubscriptionRoleId = roles.FirstOrDefault()?.Id;
		newSubscriptionUserId = null;
		newSubscriptionSeverity = "Info";
	}

	private void OnTargetTypeChanged(string type)
	{
		newSubscriptionTargetType = type;
		if (type == "role")
		{
			newSubscriptionUserId = null;
			newSubscriptionRoleId ??= roles.FirstOrDefault()?.Id;
		}
		else
		{
			newSubscriptionRoleId = null;
			newSubscriptionUserId ??= activeUsers.FirstOrDefault()?.Id;
		}
	}

	private async Task CreateSubscription()
	{
		if (newSubscriptionEventId == null) return;
		if (newSubscriptionTargetType == "role" && newSubscriptionRoleId == null) return;
		if (newSubscriptionTargetType == "user" && newSubscriptionUserId == null) return;

		isCreating = true;
		try
		{
			var dto = new NotificationSubscribeRequestDto
			{
				EventId = newSubscriptionEventId.Value,
				RoleId = newSubscriptionTargetType == "role" ? newSubscriptionRoleId : null,
				UserId = newSubscriptionTargetType == "user" ? newSubscriptionUserId : null,
				Severity = newSubscriptionSeverity
			};

			var id = await AdminService.CreateSubscriptionAsync(dto);
			if (id != null)
			{
				await LoadSubscriptions();
				CloseModals();
			}
		}
		finally
		{
			isCreating = false;
		}
	}

	private async Task ToggleSubscription(Guid id)
	{
		await AdminService.ToggleSubscriptionAsync(id);
		await LoadSubscriptions();
	}

	private async Task ChangeSeverity(Guid id, string severity)
	{
		var ok = await AdminService.UpdateSubscriptionSeverityAsync(id, severity);
		if (ok) await LoadSubscriptions();
	}

	private void ConfirmDeleteSubscription(NotificationSubscriptionDto sub)
	{
		subscriptionToDelete = sub.Id;
		subscriptionToDeleteLabel = sub.UserId != null
			? $"пользователя «{sub.UserName}»"
			: $"роли «{sub.RoleName}»";
		showDeleteSubscriptionModal = true;
		CloseOtherModalsExcept(nameof(showDeleteSubscriptionModal));
	}

	private async Task DeleteSubscription()
	{
		if (subscriptionToDelete == null) return;
		var ok = await AdminService.DeleteSubscriptionAsync(subscriptionToDelete.Value);
		if (ok) await LoadSubscriptions();
		showDeleteSubscriptionModal = false;
		subscriptionToDelete = null;
		subscriptionToDeleteLabel = "";
	}
}