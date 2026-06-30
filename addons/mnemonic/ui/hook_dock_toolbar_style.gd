class_name HookDockToolbarStyle
extends RefCounted

const HookDockVerticalLayoutGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_vertical_layout.gd"
)


static func apply_full_width_button(theme: HookDockTheme, btn: Button) -> void:
	if theme != null:
		theme.apply_full_width_button(btn)
	else:
		btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL


static func apply_disclosure_button(theme: HookDockTheme, btn: Button) -> void:
	if theme != null:
		theme.apply_disclosure_button(btn)
	else:
		btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL


static func apply_toolbar_icon_button(theme: HookDockTheme, btn: Button) -> void:
	btn.icon_alignment = HORIZONTAL_ALIGNMENT_CENTER
	btn.vertical_icon_alignment = VERTICAL_ALIGNMENT_CENTER
	if theme != null:
		theme.apply_body_font(btn)
		btn.theme_type_variation = &""


static func apply_header_transport_button(theme: HookDockTheme, btn: Button) -> void:
	if theme != null:
		theme.apply_body_font(btn)
	else:
		btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.size_flags_vertical = Control.SIZE_SHRINK_CENTER


static func apply_dock_label(label: Label) -> void:
	HookDockVerticalLayoutGd.apply_label_center(label)
