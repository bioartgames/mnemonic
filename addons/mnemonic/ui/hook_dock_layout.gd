class_name HookDockLayout
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

static var _editor_interface: EditorInterface = null


static func ensure_editor_setting_registered(editor_interface: EditorInterface) -> void:
	if editor_interface == null:
		return
	_editor_interface = editor_interface
	var settings := editor_interface.get_editor_settings()
	if settings.has_setting(Mc.EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET):
		return
	settings.set_setting(Mc.EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET, Mc.DOCK_SPLIT_UNSET)


static func read_split_offset() -> int:
	if _editor_interface == null:
		return Mc.DOCK_SPLIT_UNSET
	var settings := _editor_interface.get_editor_settings()
	if not settings.has_setting(Mc.EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET):
		return Mc.DOCK_SPLIT_UNSET
	return int(settings.get_setting(Mc.EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET))


static func write_split_offset(offset_px: int) -> void:
	if _editor_interface == null:
		return
	var settings := _editor_interface.get_editor_settings()
	settings.set_setting(Mc.EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET, offset_px)


static func clamp_split_offset(
	offset_px: int,
	total_height_px: int,
	min_top_px: int,
	min_bottom_px: int,
) -> int:
	if total_height_px <= 0:
		return min_top_px
	var max_offset := total_height_px - min_bottom_px
	if max_offset < min_top_px:
		return min_top_px
	return clampi(offset_px, min_top_px, max_offset)


static func collapsed_clips_drawer_top_px(total_height_px: int, min_bottom_px: int) -> int:
	return clamp_split_offset(
		total_height_px - min_bottom_px,
		total_height_px,
		Mc.DOCK_SPLIT_TOP_MIN_PX,
		min_bottom_px,
	)


static func is_clips_drawer_at_bottom(
	current_top_px: int,
	total_height_px: int,
	min_bottom_px: int,
	tolerance_px: int = Mc.DOCK_SPLIT_COLLAPSED_TOLERANCE_PX,
) -> bool:
	var collapsed := collapsed_clips_drawer_top_px(total_height_px, min_bottom_px)
	return absi(current_top_px - collapsed) <= tolerance_px


static func clips_drawer_double_click_top_px(
	current_top_px: int,
	total_height_px: int,
	dock_top_height_px: int,
	min_bottom_px: int,
	tolerance_px: int = Mc.DOCK_SPLIT_COLLAPSED_TOLERANCE_PX,
) -> int:
	if is_clips_drawer_at_bottom(current_top_px, total_height_px, min_bottom_px, tolerance_px):
		return clamp_split_offset(
			dock_top_height_px + Mc.DOCK_SPLIT_CHROME_GAP_PX,
			total_height_px,
			Mc.DOCK_SPLIT_TOP_MIN_PX,
			min_bottom_px,
		)
	return collapsed_clips_drawer_top_px(total_height_px, min_bottom_px)
