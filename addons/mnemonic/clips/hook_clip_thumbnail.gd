class_name HookClipThumbnail
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

const PLACEHOLDER_MODULATE := Color(0.25, 0.25, 0.28, 1.0)


static func thumb_abs_for_folder(folder_abs: String) -> String:
	return folder_abs.path_join(Mc.CLIP_THUMB_FILE_NAME)


static func try_load_texture(thumb_abs: String) -> Texture2D:
	if thumb_abs.is_empty() or not FileAccess.file_exists(thumb_abs):
		return null
	var img := Image.load_from_file(thumb_abs)
	if img == null or img.is_empty():
		return null
	return ImageTexture.create_from_image(img)


static func apply_placeholder(rect: TextureRect) -> void:
	rect.custom_minimum_size = Vector2(Mc.CLIP_THUMB_WIDTH_PX, Mc.CLIP_THUMB_HEIGHT_PX)
	rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	rect.texture = null
	rect.modulate = PLACEHOLDER_MODULATE


static func apply_to_texture_rect(rect: TextureRect, _thumb_abs: String) -> void:
	apply_placeholder(rect)
