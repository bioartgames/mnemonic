class_name HookDockVerticalLayout
extends RefCounted


static func apply_hbox_center(row: HBoxContainer) -> void:
	row.alignment = BoxContainer.ALIGNMENT_CENTER


static func apply_label_center(label: Label) -> void:
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT


static func apply_control_row_center(control: Control) -> void:
	if control is HBoxContainer:
		apply_hbox_center(control as HBoxContainer)
		return
	if control is BoxContainer:
		(control as BoxContainer).alignment = BoxContainer.ALIGNMENT_CENTER


## Pin a clip-row control to the thumb-height band so it does not independently center in a taller parent.
static func pin_to_thumb_band(control: Control, thumb_height: float) -> void:
	control.custom_minimum_size.y = thumb_height
	control.size_flags_vertical = Control.SIZE_SHRINK_BEGIN


## Thumb-height row band centered in a taller parent (PanelContainer, VBox stretch, etc.).
static func apply_thumb_band_center(control: Control, thumb_height: float) -> void:
	control.custom_minimum_size.y = thumb_height
	control.size_flags_vertical = Control.SIZE_SHRINK_CENTER


## Keep the row menu in the same vertical band as clip thumbs (avoids SHRINK_CENTER drift in tall rows).
static func wrap_menu_for_thumb_band(menu: MenuButton, thumb_size_v: Vector2) -> CenterContainer:
	var slot := CenterContainer.new()
	slot.custom_minimum_size.y = thumb_size_v.y
	slot.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	menu.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	menu.custom_minimum_size.y = thumb_size_v.y
	slot.add_child(menu)
	return slot


## Match menu thumb-band centering so countdown text fills the row height band.
static func wrap_label_for_thumb_band(label: Label, thumb_size_v: Vector2) -> CenterContainer:
	var slot := CenterContainer.new()
	slot.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slot.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	slot.custom_minimum_size.y = thumb_size_v.y
	label.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	label.custom_minimum_size.y = thumb_size_v.y
	apply_label_center(label)
	slot.add_child(label)
	return slot
