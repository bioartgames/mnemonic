class_name HookDockPopupUtils
extends RefCounted


static func set_item_disabled(menu: PopupMenu, item_id: int, disabled: bool) -> void:
	var idx := menu.get_item_index(item_id)
	if idx >= 0:
		menu.set_item_disabled(idx, disabled)


static func set_item_checked(menu: PopupMenu, item_id: int, checked: bool) -> void:
	var idx := menu.get_item_index(item_id)
	if idx >= 0:
		menu.set_item_checked(idx, checked)


static func is_item_checked(menu: PopupMenu, item_id: int) -> bool:
	var idx := menu.get_item_index(item_id)
	if idx < 0:
		return false
	return menu.is_item_checked(idx)


static func toggle_check_item(menu: PopupMenu, item_id: int) -> bool:
	var idx := menu.get_item_index(item_id)
	if idx < 0:
		return false
	menu.toggle_item_checked(idx)
	return menu.is_item_checked(idx)
