class_name HookDockSettingsMenu
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookDockPopupUtilsGd = preload("res://addons/mnemonic_hook/ui/hook_dock_popup_utils.gd")
const HookDockStatusToastGd = preload("res://addons/mnemonic_hook/ui/hook_dock_status_toast.gd")
const HookDockToolbarStyleGd = preload("res://addons/mnemonic_hook/ui/hook_dock_toolbar_style.gd")
const HookEditorWorkflowSettingsGd = preload(
	"res://addons/mnemonic_hook/ipc/hook_editor_workflow_settings.gd"
)
const HookDockHostGd = preload("res://addons/mnemonic_hook/ui/hook_dock_host.gd")

const _MENU_SETTINGS_WORKFLOW_SUBMENU := 0
const _MENU_SETTINGS_CAPTURE_PANEL := 1
const _MENU_WORKFLOW_AUTO_START_ON_OPEN := 0
const _MENU_WORKFLOW_STOP_ON_EXIT := 1
const _MENU_WORKFLOW_VERBOSE_LOGGING := 2

var host
var settings_button: Button
var settings_panel: EditorHookSettingsPanel
var popup: PopupMenu
var workflow_menu: PopupMenu
var on_capture_panel: Callable = Callable()


func setup(
	p_host,
	settings_btn: Button,
	panel: EditorHookSettingsPanel,
) -> void:
	host = p_host
	settings_button = settings_btn
	settings_panel = panel
	if is_instance_valid(settings_button):
		settings_button.pressed.connect(on_settings_button_pressed)


func build_menus(dock_node: Node) -> void:
	popup = PopupMenu.new()
	popup.name = "SettingsPopup"
	dock_node.add_child(popup)
	popup.clear()

	workflow_menu = PopupMenu.new()
	_configure_checkable_settings_menu(workflow_menu)
	workflow_menu.add_check_item(
		Mc.MENU_WORKFLOW_AUTO_START_ON_OPEN,
		_MENU_WORKFLOW_AUTO_START_ON_OPEN,
	)
	workflow_menu.add_check_item(
		Mc.MENU_WORKFLOW_STOP_ON_EXIT,
		_MENU_WORKFLOW_STOP_ON_EXIT,
	)
	workflow_menu.add_check_item(
		Mc.MENU_WORKFLOW_VERBOSE_LOGGING,
		_MENU_WORKFLOW_VERBOSE_LOGGING,
	)
	_set_menu_item_tooltip(
		workflow_menu,
		_MENU_WORKFLOW_AUTO_START_ON_OPEN,
		Mc.TOOLTIP_WORKFLOW_AUTO_START_ON_OPEN,
	)
	_set_menu_item_tooltip(
		workflow_menu,
		_MENU_WORKFLOW_STOP_ON_EXIT,
		Mc.TOOLTIP_WORKFLOW_STOP_ON_EXIT,
	)
	_set_menu_item_tooltip(
		workflow_menu,
		_MENU_WORKFLOW_VERBOSE_LOGGING,
		Mc.TOOLTIP_WORKFLOW_VERBOSE_LOGGING,
	)
	if not workflow_menu.id_pressed.is_connected(_on_workflow_menu_id_pressed):
		workflow_menu.id_pressed.connect(_on_workflow_menu_id_pressed)
	if not workflow_menu.about_to_popup.is_connected(_on_workflow_menu_about_to_popup):
		workflow_menu.about_to_popup.connect(_on_workflow_menu_about_to_popup)
	popup.add_submenu_node_item(
		Mc.MENU_WORKFLOW_SUBMENU,
		workflow_menu,
		_MENU_SETTINGS_WORKFLOW_SUBMENU,
	)
	popup.add_item(Mc.UI_CAPTURE_PANEL, _MENU_SETTINGS_CAPTURE_PANEL)
	_set_menu_item_tooltip(popup, _MENU_SETTINGS_CAPTURE_PANEL, Mc.TOOLTIP_SETTINGS_CAPTURE_PANEL)
	if not popup.id_pressed.is_connected(_on_settings_menu_id_pressed):
		popup.id_pressed.connect(_on_settings_menu_id_pressed)
	if not popup.about_to_popup.is_connected(_on_settings_menu_about_to_popup):
		popup.about_to_popup.connect(_on_settings_menu_about_to_popup)


func sync_state() -> void:
	if not is_instance_valid(popup):
		return
	var paths: MnemonicDataRootPaths = null
	if host != null and host.plugin != null:
		paths = host.plugin.get_data_root_paths()
	var paths_ok := paths != null and paths.is_valid()
	_set_popup_item_disabled(popup, _MENU_SETTINGS_CAPTURE_PANEL, not paths_ok)
	_sync_workflow_menu_state()


func apply_theme() -> void:
	apply_button_icon()


func apply_button_icon() -> void:
	if host == null or host.theme == null or not is_instance_valid(settings_button):
		return
	HookDockToolbarStyleGd.apply_toolbar_icon_button(host.theme, settings_button)
	var settings_icon: Texture2D = host.theme.icon(&"Tools")
	if settings_icon != null:
		settings_button.icon = settings_icon


func on_settings_button_pressed() -> void:
	if not is_instance_valid(popup) or not is_instance_valid(settings_button):
		return
	sync_state()
	popup.reset_size()
	var screen_pos := settings_button.get_screen_position()
	var btn_size := settings_button.size
	var menu_size := popup.size
	var pos_x := int(screen_pos.x + btn_size.x - menu_size.x)
	var pos_y := int(screen_pos.y + btn_size.y)
	popup.position = Vector2i(maxi(0, pos_x), pos_y)
	popup.popup()


func on_settings_toast(message: String) -> void:
	HookDockStatusToastGd.show(host, message)


func _sync_workflow_menu_state() -> void:
	if not is_instance_valid(workflow_menu):
		return
	_set_popup_item_checked(
		workflow_menu,
		_MENU_WORKFLOW_AUTO_START_ON_OPEN,
		HookEditorWorkflowSettingsGd.read_auto_launch_core(),
	)
	_set_popup_item_checked(
		workflow_menu,
		_MENU_WORKFLOW_STOP_ON_EXIT,
		HookEditorWorkflowSettingsGd.read_stop_core_on_editor_exit(),
	)
	_set_popup_item_checked(
		workflow_menu,
		_MENU_WORKFLOW_VERBOSE_LOGGING,
		HookEditorWorkflowSettingsGd.read_verbose_logging(),
	)


func _on_settings_menu_about_to_popup() -> void:
	sync_state()


func _on_workflow_menu_about_to_popup() -> void:
	_sync_workflow_menu_state()


func _on_settings_menu_id_pressed(id: int) -> void:
	if id != _MENU_SETTINGS_CAPTURE_PANEL:
		return
	if on_capture_panel.is_valid():
		on_capture_panel.call()


func _on_workflow_menu_id_pressed(id: int) -> void:
	if host == null or host.plugin == null:
		return
	match id:
		_MENU_WORKFLOW_AUTO_START_ON_OPEN:
			_toggle_popup_check_item(workflow_menu, _MENU_WORKFLOW_AUTO_START_ON_OPEN)
			HookEditorWorkflowSettingsGd.write_auto_launch_core(
				_is_popup_item_checked(workflow_menu, _MENU_WORKFLOW_AUTO_START_ON_OPEN)
			)
			_show_settings_toast(true, false)
		_MENU_WORKFLOW_STOP_ON_EXIT:
			_toggle_popup_check_item(workflow_menu, _MENU_WORKFLOW_STOP_ON_EXIT)
			HookEditorWorkflowSettingsGd.write_stop_core_on_editor_exit(
				_is_popup_item_checked(workflow_menu, _MENU_WORKFLOW_STOP_ON_EXIT)
			)
			_show_settings_toast(true, false)
		_MENU_WORKFLOW_VERBOSE_LOGGING:
			_toggle_popup_check_item(workflow_menu, _MENU_WORKFLOW_VERBOSE_LOGGING)
			host.plugin.set_verbose_logging(
				_is_popup_item_checked(workflow_menu, _MENU_WORKFLOW_VERBOSE_LOGGING)
			)
			_show_settings_toast(true, false)


func _show_settings_toast(ok: bool, needs_capture_restart: bool) -> void:
	if not ok:
		on_settings_toast(Mc.UI_TOAST_SETTING_SAVE_FAILED)
	elif needs_capture_restart:
		on_settings_toast(Mc.UI_TOAST_SETTINGS_SAVED_RESTART)
	else:
		on_settings_toast(Mc.UI_TOAST_SETTINGS_SAVED)


func _set_menu_item_tooltip(menu: PopupMenu, item_id: int, tooltip: String) -> void:
	var idx := menu.get_item_index(item_id)
	if idx >= 0:
		menu.set_item_tooltip(idx, tooltip)


func _set_popup_item_disabled(menu: PopupMenu, item_id: int, disabled: bool) -> void:
	HookDockPopupUtilsGd.set_item_disabled(menu, item_id, disabled)


func _configure_checkable_settings_menu(menu: PopupMenu) -> void:
	menu.hide_on_checkable_item_selection = false


func _set_popup_item_checked(menu: PopupMenu, item_id: int, checked: bool) -> void:
	HookDockPopupUtilsGd.set_item_checked(menu, item_id, checked)


func _is_popup_item_checked(menu: PopupMenu, item_id: int) -> bool:
	return HookDockPopupUtilsGd.is_item_checked(menu, item_id)


func _toggle_popup_check_item(menu: PopupMenu, item_id: int) -> bool:
	return HookDockPopupUtilsGd.toggle_check_item(menu, item_id)
