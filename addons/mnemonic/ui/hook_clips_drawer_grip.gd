@tool
extends Control

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

var _dock_theme = null


func setup(dock_theme) -> void:
	_dock_theme = dock_theme
	_update_minimum_size()
	queue_redraw()


func _update_minimum_size() -> void:
	var scale: float = 1.0
	if _dock_theme != null:
		scale = _dock_theme.get_scale()
	var grip_h := maxi(Mc.GRIP_HEIGHT_PX, int(float(Mc.GRIP_HEIGHT_PX) * scale))
	custom_minimum_size = Vector2(0, grip_h)


func _draw() -> void:
	if _dock_theme == null:
		return
	var scale: float = _dock_theme.get_scale()
	var line_w := maxi(Mc.GRIP_HANDLE_WIDTH_PX, int(float(Mc.GRIP_HANDLE_WIDTH_PX) * scale))
	var line_h := maxi(1, int(float(Mc.GRIP_HANDLE_HEIGHT_PX) * scale))
	var color: Color = _dock_theme.color_editor(&"font_disabled_color")
	var center_x := size.x * 0.5
	var center_y := size.y * 0.5
	var rect := Rect2(center_x - line_w * 0.5, center_y - line_h * 0.5, line_w, line_h)
	draw_rect(rect, color)
